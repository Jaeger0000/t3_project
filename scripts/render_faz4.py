"""Faz 4 ekranlarının gerçek tarayıcıda render doğrulaması.

Uçtan uca betik 'sunucu doğru yanıtı verdi'yi kanıtlar; bu betik 'ekranda
doğru şey yazıyor'u. Faz 4'ün asıl sözü burada görünür hâle geliyor: aynı
başarı kaydı satırı Süper Yönetici'de tutarıyla, Karar Verici'de kilitle
render oluyor.
"""

import json
import sys
import urllib.request

sys.path.insert(0, __file__.rsplit("/", 1)[0])
from cdp import Browser  # noqa: E402

APP = "http://localhost:5173"
API = "http://localhost:5080"
PW = "T3.Creathon!2026"

ok = 0
failures = []


def check(label, condition, detail=""):
    global ok
    if condition:
        ok += 1
        print(f"  ok   {label}")
    else:
        failures.append(f"{label} — {detail}")
        print(f"  FAIL {label} — {detail[:400]}")


def api(path, token=None):
    req = urllib.request.Request(API + path)
    if token:
        req.add_header("Authorization", "Bearer " + token)
    with urllib.request.urlopen(req) as r:
        return json.load(r)


def post(path, token, body):
    data = json.dumps(body).encode()
    req = urllib.request.Request(API + path, data=data, method="POST")
    req.add_header("Content-Type", "application/json")
    req.add_header("Authorization", "Bearer " + token)
    with urllib.request.urlopen(req) as r:
        return json.load(r)


def login(email):
    body = json.dumps({"email": email, "password": PW}).encode()
    req = urllib.request.Request(API + "/api/auth/login", data=body, method="POST")
    req.add_header("Content-Type", "application/json")
    with urllib.request.urlopen(req) as r:
        return json.load(r)["accessToken"]


tokens = {
    "admin": login("admin@t3ekosistem.test"),
    "kulucka": login("kulucka.yoneticisi@t3ekosistem.test"),
    "karar": login("karar.verici@t3ekosistem.test"),
    "portal": login("girisim@t3ekosistem.test"),
}

startup_id = api("/api/me", tokens["portal"])["startupId"]

# Ekranda aranacak ham tutar metnini API'den okuyoruz: sabit yazmak, tohum
# verisi değiştiğinde sessizce yanlış doğrulayan bir kontrol bırakırdı.
achievements = api(f"/api/startups/{startup_id}/achievements", tokens["admin"])["items"]
money_row = next(a for a in achievements if a["amount"] is not None)
raw_amount = f"{money_row['amount']:,.0f}".replace(",", ".")

documents = api(f"/api/startups/{startup_id}/documents", tokens["admin"])["items"]
doc_name = documents[0]["fileName"]

def pending(target_type):
    """Kuyruktan verilen türde bekleyen bir öneri döner; yoksa portaldan üretir.

    Uçtan uca betikler kuyruktaki tohum önerilerini karara bağlıyor. Bu betiğin
    onlardan sonra da çalışabilmesi için eksik öneriyi kendisi gönderiyor —
    aksi hâlde çalıştırma sırası sessiz bir ön koşula dönüşürdü.
    """
    rows = api("/api/change-requests?status=Pending&pageSize=100",
               tokens["kulucka"])["page"]["items"]
    found = next((i for i in rows if i["targetType"] == target_type), None)
    if found:
        return found

    if target_type == "Achievement":
        post("/api/change-requests", tokens["portal"], {
            "targetType": "Achievement", "operation": "Create", "targetId": None,
            "startup": None, "teamMember": None,
            "achievement": {
                "kind": "Revenue", "occurredOn": "2025-12-31",
                "note": None, "amount": 18400000, "currency": "TRY",
                "fiscalYear": 2025, "quarter": None,
                "roundType": None, "valuation": None, "investorNames": None,
                "institution": None, "programName": None,
                "awardName": None, "organization": None, "rank": None,
                "targetCountries": None,
            }})
    else:
        upload_document("anadolu_robotik_2025_faaliyet_raporu.pdf")

    rows = api("/api/change-requests?status=Pending&pageSize=100",
               tokens["kulucka"])["page"]["items"]
    return next(i for i in rows if i["targetType"] == target_type)


def upload_document(file_name):
    """Portal kullanıcısı adına dosya yükler; öneri sunucuda üretilir."""
    boundary = "----t3render"
    body = b"".join([
        f"--{boundary}\r\n".encode(),
        f'Content-Disposition: form-data; name="file"; filename="{file_name}"\r\n'.encode(),
        b"Content-Type: application/octet-stream\r\n\r\n",
        b"%PDF-1.4\n% kurgu demo dosyasi\n%%EOF\n",
        f"\r\n--{boundary}--\r\n".encode(),
    ])
    req = urllib.request.Request(
        f"{API}/api/startups/{startup_id}/documents?type=Report", data=body, method="POST")
    req.add_header("Content-Type", f"multipart/form-data; boundary={boundary}")
    req.add_header("Authorization", "Bearer " + tokens["portal"])
    with urllib.request.urlopen(req) as r:
        return json.load(r)


achievement_req = pending("Achievement")
document_req = pending("Document")

detail = api(f"/api/change-requests/{achievement_req['id']}", tokens["kulucka"])
proposed_amount = next(f for f in detail["fields"] if f["field"] == "amount")["after"]

browser = Browser()


# React denetimli girdilere doğrudan `value` atamak işe yaramıyor: React kendi
# değer takipçisini kullanıyor ve olay tetiklenmezse durumu güncellenmiyor.
# Yerel setter'ı çağırıp `input`/`change` olaylarını elle yaymak gerekiyor.
FILL_JS = """
(() => {
  function setValue(el, value) {
    const proto = el instanceof HTMLSelectElement
      ? window.HTMLSelectElement.prototype
      : window.HTMLInputElement.prototype;
    Object.getOwnPropertyDescriptor(proto, 'value').set.call(el, value);
    el.dispatchEvent(new Event('input', { bubbles: true }));
    el.dispatchEvent(new Event('change', { bubbles: true }));
  }
  function byLabel(text) {
    const label = [...document.querySelectorAll('label')].find(
      (l) => ((l.querySelector('span') || {}).innerText || '').trim().startsWith(text));
    return label ? label.querySelector('input, select') : null;
  }
  const missing = [];
  for (const [label, value] of Object.entries(__FIELDS__)) {
    const el = byLabel(label);
    if (!el) { missing.push(label); continue; }
    setValue(el, value);
  }
  return missing.length ? 'eksik alan: ' + missing.join(', ') : 'ok';
})()
"""


def fill(fields):
    """Etiketine göre form alanlarını doldurur; eksik alan varsa adını döner."""
    return browser.evaluate(FILL_JS.replace("__FIELDS__", json.dumps(fields)))


def as_user(role, path, wait_for=None):
    """Jetonu yerleştirip hedef rotayı açar ve görünen metni döner."""
    browser.goto(f"{APP}/giris")
    browser.evaluate(
        f"localStorage.setItem('t3.accessToken', {json.dumps(tokens[role])})")
    return browser.goto(f"{APP}{path}", wait_for=wait_for)


try:
    print("\n=== Başarı kayıtları sekmesi ===")
    as_user("admin", f"/girisimler/{startup_id}", wait_for="Başarılar")
    text = browser.click_text("Başarılar", wait_for="Başarı ve finans")
    check("başarı kayıtları sekmesi render oluyor",
          text and "Başarı ve finans kayıtları" in text, (text or "")[:300])
    check("kayıt sayısı başlıkta", "kayıt" in (text or ""), (text or "")[:200])
    check("süper yönetici tutarı ekranda görüyor", raw_amount in (text or ""),
          f"'{raw_amount}' metinde yok: {(text or '')[:400]}")
    check("doğrulanmış kayıt rozeti görünüyor", "Doğrulandı" in (text or ""),
          (text or "")[:300])
    check("yetkili için kayıt ekleme düğmesi var", "Kayıt ekle" in (text or ""),
          (text or "")[:300])
    check("maskeleme uyarısı yetkilide çıkmıyor",
          "tutarları görmez" not in (text or ""), (text or "")[:300])

    as_user("karar", f"/girisimler/{startup_id}", wait_for="Başarılar")
    text = browser.click_text("Başarılar", wait_for="Başarı ve finans")
    check("karar verici de kayıt satırlarını görüyor",
          text and money_row["title"] in text, (text or "")[:400])
    check("karar vericide tutar ekranda YOK", raw_amount not in (text or ""),
          f"'{raw_amount}' ekranda görünüyor")
    check("tutar yerine 'yetkiniz yok' yazıyor", "yetkiniz yok" in (text or ""),
          (text or "")[:400])
    check("maskeleme gerekçesi ekranda", "tutarları görmez" in (text or ""),
          (text or "")[:400])
    check("karar vericide kayıt ekleme düğmesi yok",
          "Kayıt ekle" not in (text or ""), (text or "")[:300])
    check("maskelenen tutar 0 ₺ olarak görünmüyor",
          "0 ₺" not in (text or ""), (text or "")[:400])

    print("\n=== Doküman sekmesi ===")
    as_user("admin", f"/girisimler/{startup_id}", wait_for="Dokümanlar")
    text = browser.click_text("Dokümanlar", wait_for="dosya")
    check("doküman listesi render oluyor", text and doc_name in text,
          (text or "")[:400])
    check("indirme düğmesi var", "İndir" in (text or ""), (text or "")[:300])
    check("yükleme formu yetkiliye açık", "Yükle" in (text or ""), (text or "")[:300])
    check("boyut sınırı ekranda yazıyor", "20 MB" in (text or ""), (text or "")[:400])

    as_user("karar", f"/girisimler/{startup_id}", wait_for="Dokümanlar")
    text = browser.click_text("Dokümanlar", wait_for="yetkiniz yok")
    check("karar vericide doküman listesi kapalı",
          "Doküman görüntüleme yetkiniz yok" in (text or ""), (text or "")[:400])
    check("karar vericide dosya adı ekranda YOK", doc_name not in (text or ""),
          f"'{doc_name}' ekranda görünüyor")

    print("\n=== Portal: öneri yolu ===")
    text = as_user("portal", "/portal", wait_for="Başarı ve finans")
    check("portalda başarı kaydı bölümü var",
          "Başarı ve finans kayıtları" in text, text[:300])
    check("portalda düğme 'Kayıt öner'",
          "Kayıt öner" in text and "Kayıt ekle" not in text, text[:400])
    check("portalda onay uyarısı yazıyor",
          "onaydan sonra yayına girer" in text, text[:400])
    check("portalda doküman yükleme var", "Dokümanlar" in text, text[:300])
    check("portalda dokümanın onaya gideceği yazıyor",
          "onaydan sonra listeye girer" in text, text[:600])

    print("\n=== Onay ekranı: yeni hedef türleri ===")
    text = as_user("kulucka", f"/onaylar/{achievement_req['id']}", wait_for="karşılaştırma")
    check("başarı kaydı önerisi diff olarak render oluyor",
          "Değişiklik karşılaştırması" in text, text[:300])
    check("tutar satırı ekranda", "Tutar" in text, text[:400])
    check("önerilen tutar ekranda görünüyor", proposed_amount in text,
          f"'{proposed_amount}' metinde yok: {text[:400]}")
    check("kayıt türü satırı ekranda", "Kayıt türü" in text, text[:400])

    text = as_user("kulucka", f"/onaylar/{document_req['id']}", wait_for="karşılaştırma")
    check("doküman önerisi diff olarak render oluyor",
          "Dosya adı" in text, text[:400])
    proposed_name = api(f"/api/change-requests/{document_req['id']}", tokens["kulucka"])
    proposed_name = next(
        f for f in proposed_name["fields"] if f["field"] == "fileName")["after"]
    check("önerilen dosya adı ekranda", proposed_name in text, text[:400])
    check("doküman boyutu ekranda", "Boyut" in text, text[:400])
    check("depo yolu ekranda sızmıyor", "storage" not in text.lower(), text[:400])

    print("\n=== Portal: formdan öneri gönderme ===")
    before = len(api("/api/change-requests?status=Pending&pageSize=100",
                     tokens["kulucka"])["page"]["items"])

    as_user("portal", "/portal", wait_for="Kayıt öner")
    text = browser.click_text("Kayıt öner", wait_for="Kayıt türü")
    check("öneri formu açılıyor", text and "Kayıt türü" in text, (text or "")[:300])

    # Tür değişimi alan kümesini değiştiriyor; iki adımda dolduruluyor.
    check("kayıt türü seçilebiliyor", fill({"Kayıt türü": "Grant"}) == "ok",
          str(fill({"Kayıt türü": "Grant"})))
    text = browser.text()
    check("hibe alanları forma geliyor", "Destek veren kurum" in text, text[:400])

    result = fill({"Tutar": "1250000", "Destek programı": "KOBİGEL 2025",
                   "Tarih": "2025-04-10"})
    check("hibe alanları doldurulabiliyor", result == "ok", str(result))

    # Kurum açılırına hiç dokunulmuyor: ekranda seçili görünen varsayılan
    # gövdeye de yazılmış olmalı. Yazılmasaydı sunucu 400 döner ve öneri
    # kuyruğa girmezdi — bu kontrol tam olarak onu yakalıyor.
    text = browser.click_text("Onaya gönder", wait_for="onay kuyruğuna")
    check("öneri gönderildi mesajı ekranda",
          text and "onay kuyruğuna alındı" in text, (text or "")[:400])

    after = api("/api/change-requests?status=Pending&pageSize=100",
                tokens["kulucka"])["page"]["items"]
    grant = next((i for i in after
                  if i["targetType"] == "Achievement"
                  and "Hibe" in i["targetLabel"]), None)
    check("öneri gerçekten kuyruğa girdi", len(after) == before + 1,
          f"{before} -> {len(after)}")
    check("kuyruk satırı hibe kaydını gösteriyor", grant is not None,
          json.dumps([i["targetLabel"] for i in after], ensure_ascii=False))

    if grant:
        detail = api(f"/api/change-requests/{grant['id']}", tokens["kulucka"])
        institution = next(f for f in detail["fields"] if f["field"] == "institution")
        check("dokunulmayan açılırın varsayılanı gövdeye yazılmış",
              institution["after"] == "TÜBİTAK", str(institution["after"]))

    print("\n=== Kart özeti ===")
    text = as_user("karar", f"/girisimler/{startup_id}", wait_for="Toplam yatırım")
    check("karar vericide finansal özet maskeli",
          text.count("yetkiniz yok") >= 3, text[:500])
    check("karar vericide başarı sayısı yine de görünüyor",
          "başarı kaydı" in text, text[:500])

finally:
    browser.close()

print("\n" + "=" * 60)
print(f"{ok} render kontrolü geçti, {len(failures)} başarısız")
for f in failures:
    print("  FAIL " + f)
sys.exit(1 if failures else 0)
