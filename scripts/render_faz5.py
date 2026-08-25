"""Faz 5 ekranlarının gerçek tarayıcıda render doğrulaması.

Faz 5'in sözü karar destek: pano, grafikler, CSV ve AI paneli. Bu betik,
uçtan uca betiğin kanıtladığı "sunucu doğru sayıyı döndü"nün üstüne
"kullanıcı ekranda doğru sayıyı görüyor"u koyuyor. En kritik kontrol Karar
Verici satırı: ekosistem toplamını görüyor ama tekil girişimin tutarı yerine
ekranda kilit duruyor — bu ayrım yalnızca gerçek render'da görünür.
"""

import json
import re
import sys
import time
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
        print(f"  FAIL {label} — {str(detail)[:400]}")


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
    "karar": login("karar.verici@t3ekosistem.test"),
    "portal": login("girisim@t3ekosistem.test"),
}

# Beklenen sayılar API'den okunuyor: sabit yazılsaydı tohum verisi
# büyüdüğünde kontrol sessizce yanlış şeyi doğrulamaya devam ederdi.
stats = {role: api("/api/reports/ecosystem", token) for role, token in tokens.items()}
portal_startup = api("/api/me", tokens["portal"])["startupId"]
top_admin = stats["admin"]["topByInvestment"][0]

browser = Browser()


def as_user(role, path, wait_for=None):
    browser.goto(f"{APP}/giris")
    browser.set_session(tokens[role])
    return browser.goto(f"{APP}{path}", wait_for=wait_for)


def testid_text(testid, timeout=15):
    """`data-testid` taşıyan öğenin metnini bekleyip döner.

    Beklemeyi metne değil öğeye bağlamak gerekiyor: başlıklar CSS ile büyük
    harfe çevrildiği için `innerText` üzerinden "Kaynaklar" aramak sessizce
    zaman aşımına uğruyordu.
    """
    query = ("(document.querySelector('[data-testid=" + json.dumps(testid)
             + "]') || {}).innerText || ''")
    deadline = time.time() + timeout
    while time.time() < deadline:
        value = browser.evaluate(query)
        if value and value.strip():
            return value
        time.sleep(0.3)
    return browser.evaluate(query) or ""


def fmt(amount, compact=False):
    """Tutarı tarayıcının kendi Intl'ıyla biçimlendirir.

    Beklenen metni Python'da elle kurmak, ekrandaki biçimden bağımsız bir
    ikinci gerçek üretirdi; ayrıştıkları anda kontrol yalan söylerdi.
    """
    options = ("{notation:'compact', maximumFractionDigits:1}" if compact
               else "{maximumFractionDigits:0}")
    text = browser.evaluate(
        f"new Intl.NumberFormat('tr-TR', {options}).format({amount})")
    return f"{text} ₺"


FETCH_CSV = """
(async () => {
  // Kimlik HttpOnly çerezde: başlığa jeton koymak gerekmiyor, tarayıcı çerezi
  // aynı origin isteğine kendisi ekliyor.
  const res = await fetch('/api/reports/export', { credentials: 'same-origin' });
  // `res.text()` BOM'u çözerken yutuyor; Excel'in ihtiyaç duyduğu baytı
  // görmek için ham tampona bakmak gerekiyor.
  const buffer = await res.arrayBuffer();
  const bytes = new Uint8Array(buffer);
  const text = new TextDecoder('utf-8').decode(buffer);
  return JSON.stringify({
    status: res.status,
    type: res.headers.get('content-type'),
    nosniff: res.headers.get('x-content-type-options'),
    bom: bytes[0] === 0xEF && bytes[1] === 0xBB && bytes[2] === 0xBF,
    lines: text.trim().split('\\n').length,
    head: text.slice(0, 300),
    body: text,
  });
})()
"""


try:
    print("\n=== Pano: süper yönetici ===")
    text = as_user("admin", "/pano", wait_for="Ekosistem panosu")
    totals = stats["admin"]["totals"]
    check("pano render oluyor", "Ekosistem panosu" in text, text[:300])
    check("girişim sayısı KPI'da", str(totals["startups"]) in text, text[:400])
    check("faal girişim ipucu ekranda",
          f"{totals['activeStartups']} faal" in text, text[:400])
    invested = fmt(totals["totalInvestment"])
    check("toplam yatırım ekranda", invested in text, f"'{invested}' yok: {text[:400]}")
    check("toplam hibe ekranda", fmt(totals["totalGrant"]) in text, text[:400])
    check("yetkilide hiçbir KPI kilitli değil", "🔒" not in text, text[:400])

    for title in ["Sektör dağılımı", "Program başına girişim", "Yatırım turu dağılımı",
                  "Yıllara göre yatırım", "Şehir dağılımı",
                  "En çok yatırım alan girişimler"]:
        check(f"grafik başlığı: {title}", title in text, text[:500])

    # Halka SVG, çubuklar genişliği satır içi verilen kutular: ikisi de
    # "metin var ama görsel çizilmedi" hâlini yakalamak için sayılıyor.
    slices = browser.evaluate("document.querySelectorAll('svg circle').length")
    check("halka grafik dilimleri çiziliyor",
          (slices or 0) >= len(stats["admin"]["bySector"]), str(slices))
    bars = browser.evaluate(
        "[...document.querySelectorAll('span[style]')]"
        ".filter(n => n.style.width).length")
    check("çubuk grafiklerde çubuklar çiziliyor", (bars or 0) >= 10, str(bars))

    check("CSV düğmesi ekranda", "CSV dışa aktar" in text, text[:400])
    check("en çok yatırım alan girişim listede", top_admin["name"] in text, text[:600])
    top_amount = fmt(top_admin["investment"], compact=True)
    check("yetkilide tekil tutar ekranda", top_amount in text,
          f"'{top_amount}' yok: {text[:600]}")

    print("\n=== Pano: karar verici (agregat açık, tekil kapalı) ===")
    text = as_user("karar", "/pano", wait_for="Ekosistem panosu")
    check("karar verici ekosistem toplamını görüyor", invested in text, text[:400])
    check("karar vericide sıralama görünüyor", top_admin["name"] in text, text[:600])
    check("karar vericide tekil tutar ekranda YOK", top_amount not in text,
          f"'{top_amount}' ekranda görünüyor")
    locks = text.count("🔒")
    expected_locks = len(stats["karar"]["topByInvestment"])
    check("her sıralama satırında kilit var", locks >= expected_locks,
          f"{locks} kilit, {expected_locks} satır bekleniyordu")
    # Basit `"0 ₺" in text` yanlış alarm verir: "649.150.000 ₺" de bu metni
    # içeriyor. Aranan şey tek başına duran sıfır.
    check("maskeli tutar 0 ₺ olarak görünmüyor",
          re.search(r"(?<![\d.,])0 ₺", text) is None, text[:500])
    check("API de tekil tutarı vermiyor",
          all(r["investment"] is None for r in stats["karar"]["topByInvestment"]),
          json.dumps(stats["karar"]["topByInvestment"][:2], ensure_ascii=False))

    print("\n=== Pano: program yöneticisi kapsamı ===")
    text = as_user("kulucka", "/pano", wait_for="Ekosistem panosu")
    scoped = stats["kulucka"]["totals"]
    check("kapsam açıklaması ekranda",
          "Sorumlu olduğunuz programların karnesi" in text, text[:300])
    check("pano yalnızca kapsamdaki girişimi sayıyor",
          str(scoped["startups"]) in text and scoped["startups"] < totals["startups"],
          f"{scoped['startups']} / {totals['startups']}")
    check("kapsam dışı toplam ekranda YOK", invested not in text,
          f"'{invested}' ekranda görünüyor")
    outside = {s["label"] for s in stats["admin"]["byProgram"]} - {
        s["label"] for s in stats["kulucka"]["byProgram"]}
    for label in outside:
        check(f"kapsam dışı program grafikte yok: {label}", label not in text, text[:600])

    print("\n=== CSV dışa aktarma (tarayıcıdan) ===")
    payload = json.loads(browser.evaluate(FETCH_CSV))
    check("CSV isteği 200 dönüyor", payload["status"] == 200, str(payload["status"]))
    check("içerik türü CSV", "text/csv" in (payload["type"] or ""), str(payload["type"]))
    check("nosniff başlığı var", payload["nosniff"] == "nosniff", str(payload["nosniff"]))
    check("Excel için BOM yazılmış", payload["bom"], payload["head"][:80])
    check("ayraç noktalı virgül", ";" in payload["head"], payload["head"][:120])
    check("başlık satırı Türkçe", "Girişim;" in payload["head"], payload["head"][:120])
    check("satır sayısı kapsamla uyuşuyor",
          payload["lines"] == scoped["startups"] + 1,
          f"{payload['lines']} satır, {scoped['startups']} + başlık bekleniyordu")

    as_user("karar", "/pano", wait_for="Ekosistem panosu")
    masked = json.loads(browser.evaluate(FETCH_CSV))
    check("karar vericinin CSV'si de geliyor", masked["status"] == 200, str(masked["status"]))
    check("CSV'de maskeli hücre 'yetkiniz yok' yazıyor",
          "yetkiniz yok" in masked["body"], masked["head"][:200])
    check("CSV'de tekil tutar sızmıyor",
          str(int(top_admin["investment"])) not in masked["body"].replace(".", ""),
          "ham tutar CSV içinde")

    text = browser.text()
    browser.click_text("⬇ CSV dışa aktar", wait_for="CSV dışa aktar")
    check("CSV düğmesi hata üretmiyor", "Dosya indirilemedi" not in browser.text(),
          browser.text()[:300])

    print("\n=== AI karar destek paneli ===")
    text = as_user("admin", "/pano", wait_for="Ekosisteme soru sor")
    check("asistan paneli panoda", "Ekosisteme soru sor" in text, text[:300])
    check("yetki uyarısı panelde",
          "yalnızca sizin görme yetkiniz olan kayıtlardan" in text, text[:500])

    text = browser.click_text("Savunma sektöründe kaç girişim var?",
                              wait_for="search_startups")
    answer = testid_text("assistant-answer")
    check("yanıt bloğu render oluyor", bool(answer and answer.strip()), str(answer)[:200])
    check("yanıt savunma sektöründen bahsediyor", "Savunma" in (answer or ""),
          str(answer)[:300])
    sources = browser.evaluate(
        "[...document.querySelectorAll('[data-testid=\"assistant-panel\"] code')]"
        ".map(n => n.innerText).join(',')")
    check("kaynaklar başlığı ekranda", "KAYNAKLAR" in (text or "").upper(),
          (text or "")[:400])
    check("kaynak araç adları ekranda", bool(sources and "search_startups" in sources),
          str(sources))
    check("hangi modun yanıtladığı rozetle söyleniyor",
          "Yerel plan (model yok)" in (text or "") or "Model:" in (text or ""),
          (text or "")[:400])

    print("\n=== Girişim kartında yönetici özeti ===")
    as_user("admin", f"/girisimler/{portal_startup}", wait_for="Yönetici özeti")
    text = browser.click_text("✨ Özet oluştur", wait_for="•")
    summary = testid_text("ai-summary-text")
    check("özet metni render oluyor", bool(summary and summary.strip()), str(summary)[:200])
    # Başlıklar CSS ile büyütülüyor; innerText render edilmiş hâli döndürdüğü
    # için karşılaştırma da büyük harf üzerinden yapılıyor.
    check("özetin dayanağı listeleniyor", "DAYANAK" in (text or "").upper(),
          (text or "")[:400])
    check("yetkilide maskeleme notu yok",
          "özet tutar içermez" not in (text or ""), (text or "")[:400])

    as_user("karar", f"/girisimler/{portal_startup}", wait_for="Yönetici özeti")
    text = browser.click_text("✨ Özet oluştur", wait_for="•")
    masked_summary = testid_text("ai-summary-text")
    check("karar vericide de özet üretiliyor",
          bool(masked_summary and masked_summary.strip()), str(masked_summary)[:200])
    check("özette tutar yok", "₺" not in (masked_summary or ""), str(masked_summary)[:300])
    check("özette maskeleme gerekçesi yazıyor",
          "özet tutar içermez" in (text or ""), (text or "")[:500])

    print("\n=== Gezinme ===")
    text = as_user("portal", "/", wait_for="Pano")
    check("pano menüde ilk sırada", "Pano" in text, text[:300])
    check("girişim kullanıcısı da kendi karnesini görüyor",
          "Girişiminizin ekosistem karnesi" in as_user("portal", "/pano",
                                                       wait_for="Ekosistem panosu"),
          "portal panosu açılmadı")

finally:
    browser.close()

print("\n" + "=" * 60)
print(f"{ok} render kontrolü geçti, {len(failures)} başarısız")
for f in failures:
    print("  FAIL " + f)
sys.exit(1 if failures else 0)
