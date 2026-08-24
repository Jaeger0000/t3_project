"""Faz 3 ekranlarının gerçek tarayıcıda render doğrulaması.

API testleri 'sunucu doğru yanıtı verdi'yi kanıtlar; bu betik 'ekranda doğru
şey yazıyor'u kanıtlar. Faz 2'deki `0 ₺` maskeleme hatasını yalnızca bu adım
yakalamıştı.
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


def login(email):
    body = json.dumps({"email": email, "password": PW}).encode()
    req = urllib.request.Request(API + "/api/auth/login", data=body, method="POST")
    req.add_header("Content-Type", "application/json")
    with urllib.request.urlopen(req) as r:
        return json.load(r)["accessToken"]


tokens = {
    "admin": login("admin@t3ekosistem.test"),
    "kulucka": login("kulucka.yoneticisi@t3ekosistem.test"),
    "teknofest": login("teknofest.yoneticisi@t3ekosistem.test"),
    "karar": login("karar.verici@t3ekosistem.test"),
    "portal": login("girisim@t3ekosistem.test"),
}

# Vergi numarası taşıyan bir öneri bul (maskeleme gösterimi için).
queue = api("/api/change-requests?pageSize=100", tokens["admin"])
tax_case = None
for item in queue["page"]["items"]:
    if item["targetType"] != "Startup":
        continue
    detail = api(f"/api/change-requests/{item['id']}", tokens["admin"])
    field = next((f for f in detail["fields"] if f["field"] == "taxNumber"), None)
    # Yalnızca gerçekten değişen alan işe yarar: diff varsayılan olarak
    # değişmeyen alanları gizliyor, maskeleme de o satırda görünüyor.
    if field and field["changed"]:
        tax_case = (item, field)
        break

kul_ids = {i["id"] for i in api("/api/change-requests?pageSize=100", tokens["kulucka"])["page"]["items"]}

browser = Browser()


def as_user(role, path, wait_for=None):
    """Jetonu yerleştirip hedef rotayı açar ve görünen metni döner."""
    browser.goto(f"{APP}/giris")
    browser.evaluate(
        f"localStorage.setItem('t3.accessToken', {json.dumps(tokens[role])})")
    return browser.goto(f"{APP}{path}", wait_for=wait_for)


try:
    print("\n=== Giriş ekranı ===")
    browser.goto(f"{APP}/giris")
    browser.evaluate("localStorage.clear()")
    text = browser.goto(f"{APP}/giris", wait_for="posta")
    check("giriş ekranı render oluyor", "posta" in text.lower(), text[:200])
    check("React uygulaması açıldı (boş gövde değil)", len(text.strip()) > 20, repr(text[:80]))

    print("\n=== Onay kuyruğu ===")
    text = as_user("kulucka", "/onaylar", wait_for="kuyru")
    check("program yöneticisi kuyruğu görüyor", "Onay kuyruğu" in text, text[:200])
    check("durum sekmeleri sayı taşıyor", "Bekleyen(" in text, text[:300])
    check("kuyrukta girişim satırı var", "değişiyor" in text or "bekliyor" in text,
          text[:400])

    text = as_user("portal", "/onaylar", wait_for="neri")
    check("girişim kullanıcısında başlık 'Önerilerim'",
          "Önerilerim" in text and "Onay kuyruğu" not in text, text[:200])

    # Dalga 1'e kadar Karar Verici burada sonsuza dek boş kalacak bir
    # "Önerilerim" ekranı görüyordu (denetim bulgusu O-03): bu rolde
    # canReviewApprovals ve mustSubmitForApproval ikisi de false. Artık
    # gerekçeyi okuyor.
    text = as_user("karar", "/onaylar", wait_for="yetkiniz yok")
    check("karar verici onay ekranında gerekçe görüyor",
          "Bu ekranı görme yetkiniz yok" in text, text[:300])

    print("\n=== Diff ekranı ve maskeleme ===")
    check("vergi numarası taşıyan öneri bulundu", tax_case is not None)
    if tax_case:
        item, field = tax_case
        raw = field["after"] or field["before"]
        owner = "kulucka" if item["id"] in kul_ids else "teknofest"

        text = as_user("admin", f"/onaylar/{item['id']}", wait_for="karşılaştırma")
        check("süper yöneticide diff tablosu render oluyor",
              "Değişiklik karşılaştırması" in text, text[:300])
        check("süper yönetici ham vergi numarasını ekranda görüyor",
              raw in text, f"'{raw}' metinde yok")

        text = as_user(owner, f"/onaylar/{item['id']}", wait_for="karşılaştırma")
        check("program yöneticisinde diff tablosu render oluyor",
              "Değişiklik karşılaştırması" in text, text[:300])
        check("program yöneticisi ham vergi numarasını GÖRMÜYOR",
              raw not in text, f"'{raw}' ekranda görünüyor — maskeleme sızdırdı")
        check("maskeli alan 'yetkiniz yok' olarak gösteriliyor",
              "yetkiniz yok" in text, text[:500])
        check("maskeli alanın etiketi yine de listeleniyor",
              "Vergi" in text or "vergi" in text, text[:500])

        text = as_user("karar", f"/onaylar/{item['id']}", wait_for="")
        check("karar verici öneri detayına erişemiyor",
              "yetkiniz yok" in text.lower() or "bulunamadı" in text.lower(), text[:300])

    print("\n=== Girişim portalı ===")
    text = as_user("portal", "/portal", wait_for="portal")
    check("portal ekranı render oluyor", "Girişim portalı" in text, text[:300])
    check("onay uyarısı görünür",
          "onayından sonra yayına girer" in text, text[:400])
    check("profil formu açık", "Girişim adı" in text or "Girişim profili" in text,
          text[:400])
    check("ekip bölümü var", "Ekip" in text and "Yeni üye öner" in text, text[:400])

    text = as_user("kulucka", "/portal", wait_for="")
    check("program yöneticisi portala giremiyor",
          "görme yetkiniz yok" in text, text[:300])

    print("\n=== Kullanıcı yönetimi ve denetim izi ===")
    text = as_user("admin", "/kullanicilar", wait_for="Kullanıcılar")
    check("kullanıcı listesi render oluyor", "Kullanıcılar" in text, text[:200])
    check("hesap satırları görünüyor", "@t3ekosistem.test" in text, text[:400])
    check("rol etiketleri Türkçe", "Program Yöneticisi" in text, text[:400])

    text = as_user("admin", "/denetim", wait_for="Denetim")
    check("denetim izi render oluyor", "Denetim izi" in text, text[:200])
    check("eylem satırları görünüyor",
          "ChangeRequest." in text or "Startup." in text or "User." in text, text[:400])

    text = as_user("kulucka", "/kullanicilar", wait_for="")
    check("program yöneticisi kullanıcı ekranını göremiyor",
          "görme yetkiniz yok" in text, text[:300])
    text = as_user("kulucka", "/denetim", wait_for="")
    check("program yöneticisi denetim izini göremiyor",
          "görme yetkiniz yok" in text, text[:300])

    print("\n=== Menü ===")
    text = as_user("admin", "/girisimler", wait_for="Girişimler")
    check("süper yönetici menüsünde kullanıcı ve denetim var",
          "Kullanıcılar" in text and "Denetim izi" in text, text[:300])
    check("süper yöneticide portal bağlantısı yok", "Girişim portalı" not in text,
          text[:300])

    text = as_user("portal", "/girisimler", wait_for="Girişimler")
    check("girişim kullanıcısı menüsünde portal var", "Girişim portalı" in text,
          text[:300])
    check("girişim kullanıcısında kullanıcı yönetimi yok", "Kullanıcılar" not in text,
          text[:300])

    text = as_user("kulucka", "/girisimler", wait_for="Girişimler")
    check("program yöneticisi menüsünde onay kuyruğu var", "Onay kuyruğu" in text,
          text[:300])
finally:
    browser.close()

print(f"\n{'=' * 60}")
print(f"geçen: {ok}   düşen: {len(failures)}")
for f in failures:
    print(f"  ✗ {f}")
sys.exit(1 if failures else 0)
