"""Dalga 0 düzeltmelerinin gerçek tarayıcıda doğrulaması.

Denetim raporunun bloklayıcılarından dördü yalnızca arayüzde görünür: girişim
yazma yolları (B-01), sekme başlığı ve sayfa dili (Y-02), 404 ekranı (Y-03) ve
telefonda yatay taşma (O-02). Mevcut render betikleri bu ekranları hiç
görmüyordu — yeni yazma formları Faz 3-5 betiklerinin kapsamında değil.

Betik veriyi **değiştirir**: yeni bir girişim oluşturur, program dönemine
bağlar, kartını düzenler ve ekip üyesi ekler. Bu yüzden en son turda çalışır.
Sonda giriş hız sınırı bölümlemesi (B-04) sınanıyor; sıra bilinçli, çünkü o
kontrol admin hesabının kovasını bir dakika boyunca doldurur.
"""

import json
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

sys.path.insert(0, __file__.rsplit("/", 1)[0])
from cdp import Browser  # noqa: E402

APP = "http://localhost:5173"
API = "http://localhost:5080"
PW = "T3.Creathon!2026"

# Ad her koşuda değişiyor: girişim adı tekil, ikinci koşu 409 alırdı.
NEW_NAME = f"Dalga0 Test Girişimi {int(time.time()) % 100000}"

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


def post(path, token, body):
    """POST; hata durumunda (durum kodu, gövde) döner."""
    data = json.dumps(body).encode()
    req = urllib.request.Request(API + path, data=data, method="POST")
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req) as r:
            return r.status, json.load(r)
    except urllib.error.HTTPError as e:
        raw = e.read().decode(errors="replace")
        try:
            return e.code, json.loads(raw)
        except json.JSONDecodeError:
            return e.code, {"raw": raw}


def put(path, token, body):
    data = json.dumps(body).encode()
    req = urllib.request.Request(API + path, data=data, method="PUT")
    req.add_header("Content-Type", "application/json")
    req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req) as r:
            return r.status, json.load(r)
    except urllib.error.HTTPError as e:
        return e.code, {"raw": e.read().decode(errors="replace")}


def login(email, password=PW):
    status, body = post("/api/auth/login", None, {"email": email, "password": password})
    return status, body


def token_of(email):
    status, body = login(email)
    if status != 200:
        raise SystemExit(f"{email} giriş yapamadı: {status} {body}")
    return body["accessToken"]


tokens = {
    "admin": token_of("admin@t3ekosistem.test"),
    "kulucka": token_of("kulucka.yoneticisi@t3ekosistem.test"),
    "karar": token_of("karar.verici@t3ekosistem.test"),
    "portal": token_of("girisim@t3ekosistem.test"),
}

portal_startup = api("/api/me", tokens["portal"])["startupId"]
kulucka_programs = api("/api/programs", tokens["kulucka"])
kulucka_program = next(p for p in kulucka_programs if p["terms"])
kulucka_term = kulucka_program["terms"][0]

# Kapsam dışı dönem: yönetici bunu arayüzde hiç görmüyor, API'ye elle vurulacak.
all_programs = api("/api/programs", tokens["admin"])
mine = {p["id"] for p in kulucka_programs}
foreign_term = next(
    (t for p in all_programs if p["id"] not in mine for t in p["terms"]), None)

browser = Browser()

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
    return browser.evaluate(FILL_JS.replace("__FIELDS__", json.dumps(fields)))


def as_user(role, path, wait_for=None):
    browser.goto(f"{APP}/giris")
    browser.set_session(tokens[role])
    return browser.goto(f"{APP}{path}", wait_for=wait_for)


def viewport(width):
    browser.call("Emulation.setDeviceMetricsOverride", width=width, height=780,
                 deviceScaleFactor=1, mobile=True)


def desktop():
    browser.call("Emulation.clearDeviceMetricsOverride")


try:
    # --- Y-02: sayfa dili ve sekme başlığı --------------------------------
    print("\n=== Sekme başlığı ve sayfa dili ===")
    as_user("admin", "/pano", wait_for="Ekosistem")
    check("sayfa dili tr", browser.evaluate("document.documentElement.lang") == "tr",
          browser.evaluate("document.documentElement.lang"))

    titles = {}
    for path, label, marker in (("/pano", "pano", "Ekosistem panosu"),
                                ("/girisimler", "girisimler", "Girişimler"),
                                ("/programlar", "programlar", "Programlar"),
                                ("/denetim", "denetim", "Denetim izi")):
        as_user("admin", path, wait_for=marker)
        titles[label] = browser.evaluate("document.title")

    check("başlık artık 'frontend' değil",
          all("frontend" not in t for t in titles.values()), json.dumps(titles))
    check("başlıkta sistem adı var",
          all("T3 Girişim Ekosistemi" in t for t in titles.values()), json.dumps(titles))
    check("her ekranın başlığı farklı", len(set(titles.values())) == 4,
          json.dumps(titles))

    as_user("admin", f"/girisimler/{portal_startup}", wait_for="Toplam yatırım")
    card_title = browser.evaluate("document.title")
    startup_name = api(f"/api/startups/{portal_startup}", tokens["admin"])["name"]
    check("girişim kartının başlığı girişim adını taşıyor",
          startup_name in card_title, card_title)

    # --- Y-03: 404 ekranı -------------------------------------------------
    print("\n=== 404 ekranı ===")
    text = as_user("admin", "/olmayan-sayfa", wait_for="bulunamadı")
    check("404 ekranı render oluyor", "Aradığınız sayfa bulunamadı" in text, text[:300])
    check("panoya sessizce yönlendirmiyor",
          browser.evaluate("location.pathname") == "/olmayan-sayfa",
          browser.evaluate("location.pathname"))
    check("404 ekranında menü duruyor (oturum açık)", "Pano" in text, text[:300])
    check("404 başlığı sekmede", "bulunamadı" in browser.evaluate("document.title"),
          browser.evaluate("document.title"))

    text = as_user("admin", "/girisimler/99999", wait_for="bulunamadı")
    check("geçersiz girişim kimliği tek ifadeyle karşılanıyor",
          "bulunamadı" in text.lower(), text[:300])

    # --- O-02: telefonda yatay taşma -------------------------------------
    print("\n=== Mobil yatay taşma ===")
    routes = [("admin", "/pano", "Ekosistem panosu"),
              ("admin", "/girisimler", "girişim"),
              ("admin", "/programlar", "Programlar"),
              ("admin", "/onaylar", "Onay kuyruğu"),
              ("admin", "/kullanicilar", "Kullanıcılar"),
              ("admin", "/denetim", "Denetim izi"),
              ("admin", "/olmayan-sayfa", "bulunamadı"),
              ("admin", f"/girisimler/{portal_startup}", "Toplam yatırım"),
              ("portal", "/portal", "Girişim profili")]
    for width in (360, 375, 414):
        viewport(width)
        for role, path, marker in routes:
            as_user(role, path, wait_for=marker)
            time.sleep(0.6)
            scroll, client = browser.evaluate(
                "[document.documentElement.scrollWidth,"
                " document.documentElement.clientWidth].join(',')").split(",")
            check(f"{width}px {path} yatay taşmıyor", int(scroll) <= int(client),
                  f"scrollWidth={scroll} clientWidth={client}")
    desktop()

    # --- B-01: girişim yazma yolları -------------------------------------
    print("\n=== Yeni girişim (Program Yöneticisi) ===")
    text = as_user("kulucka", "/girisimler", wait_for="Girişimler")
    check("yetkilide 'Yeni girişim' düğmesi var", "Yeni girişim" in text, text[:400])

    text = browser.click_text("Yeni girişim", wait_for="Girişimi oluştur")
    check("yeni girişim formu açılıyor", text and "Girişim adı" in text, (text or "")[:300])
    check("form alanları eksiksiz doldurulabiliyor",
          fill({"Girişim adı": NEW_NAME, "Sektör": "Health", "Şehir": "Kayseri",
                "İletişim e-postası": "iletisim@dalga0.test"}) == "ok",
          str(fill({"Girişim adı": NEW_NAME})))

    text = browser.click_text("Girişimi oluştur", wait_for="sıradaki adım")
    check("kayıt oluştu ve katılım adımı açıldı",
          text and "sıradaki adım" in text, (text or "")[:400])
    check("yeni kaydın adı ekranda", text and NEW_NAME in text, (text or "")[:400])
    check("kapsam kuralı ekranda açıklanıyor",
          text and "program dönemine bağlamanız" in text, (text or "")[:400])

    created = api(f"/api/startups?q={urllib.parse.quote(NEW_NAME)}", tokens["admin"])
    check("girişim veritabanına yazıldı", created["totalCount"] == 1,
          json.dumps(created["totalCount"]))
    new_id = created["items"][0]["id"] if created["items"] else None

    print("\n=== Program dönemine bağlama ===")
    # Dönem açılırı programa bağlı: program seçilmeden seçenekleri yok, bu yüzden
    # iki adımda doldurulur. Tek blokta doldurmak dönemi sessizce boş bırakıyordu.
    check("program seçilebiliyor",
          fill({"Program": kulucka_program["id"]}) == "ok", "program açılırı yok")
    time.sleep(0.5)
    check("dönem ve tarih doldurulabiliyor",
          fill({"Dönem": kulucka_term["id"], "Durum": "Accepted",
                "Katılım tarihi": "2026-02-02"}) == "ok", "dönem açılırı yok")
    check("dönem gerçekten seçildi", browser.evaluate(
        "(() => {const l = [...document.querySelectorAll('label')].find("
        "  x => (x.querySelector('span')||{}).innerText.startsWith('Dönem'));"
        " return l ? l.querySelector('select').value : 'etiket yok'})()")
        == kulucka_term["id"], "dönem seçilmedi")

    text = browser.click_text("Programa ekle", wait_for="Gelişim yolculuğu")
    check("katılım sonrası girişim kartı açılıyor",
          text and "Gelişim yolculuğu" in text and NEW_NAME in text,
          (text or "")[:300])

    card = api(f"/api/startups/{new_id}", tokens["kulucka"])
    check("girişim artık yöneticinin kapsamında", card["name"] == NEW_NAME, card["name"])
    check("katılım kaydı kartta", len(card["programs"]) == 1,
          json.dumps([p["termName"] for p in card["programs"]]))

    text = browser.click_text("Gelişim yolculuğu", wait_for=kulucka_program["name"])
    check("katılım kronolojide görünüyor",
          text and kulucka_program["name"] in text, (text or "")[:400])

    print("\n=== Kart düzenleme ===")
    # Vergi numarası maskeli alan; korunup korunmadığını sınamak için Süper
    # Yönetici olarak bir değer yazılıyor.
    seeded_tax = "1234567890"
    put(f"/api/startups/{new_id}", tokens["admin"], {
        "name": NEW_NAME, "legalName": None, "taxNumber": seeded_tax,
        "foundedOn": None, "sector": "Health", "technologyAreas": [],
        "productDescription": None, "website": None, "logoUrl": None,
        "city": "Kayseri", "contactEmail": "iletisim@dalga0.test",
        "contactPhone": None, "status": "Active"})

    as_user("kulucka", f"/girisimler/{new_id}", wait_for=NEW_NAME)
    edit_text = browser.click_text("Düzenle", wait_for="Girişim profili")
    text = edit_text
    check("düzenleme formu açılıyor", text and "Girişim profili" in text,
          (text or "")[:300])
    check("şehir güncellenebiliyor", fill({"Şehir": "Sivas"}) == "ok",
          str(fill({"Şehir": "Sivas"})))
    text = browser.click_text("Kaydet", wait_for="güncellendi")
    check("kaydetme onayı ekranda", text and "güncellendi" in text, (text or "")[:300])
    check("değişiklik sunucuya işlendi",
          api(f"/api/startups/{new_id}", tokens["kulucka"])["city"] == "Sivas",
          api(f"/api/startups/{new_id}", tokens["kulucka"])["city"])

    # Program Yöneticisi vergi numarasını göremiyor; form da göstermiyor ve
    # kaydetmek mevcut değeri silmiyor.
    print("\n=== Maskeli alan yazma yolunda korunuyor ===")
    admin_card = api(f"/api/startups/{new_id}", tokens["admin"])
    # İki kontrol de **form açıkken** alınan metne bakıyor: kaydetmeden sonra
    # form kapanıyor ve gerekçe ekrandan kalkıyor.
    check("maskeli alan formda yok (vergi numarası)",
          "Vergi numarası" not in (edit_text or ""), (edit_text or "")[:400])
    check("maskeli alanın korunacağı formda yazıyor",
          "mevcut değerleri korunur" in (edit_text or ""), (edit_text or "")[:400])
    check("düzenleme vergi numarasını silmedi",
          admin_card["taxNumber"] == seeded_tax, str(admin_card["taxNumber"]))

    print("\n=== Ekip üyesi ekleme ===")
    as_user("kulucka", f"/girisimler/{new_id}", wait_for="Ekip")
    text = browser.click_text("Ekip", wait_for="Üye ekle")
    check("yetkilide 'Üye ekle' düğmesi var", text and "Üye ekle" in text,
          (text or "")[:300])
    form_text = browser.click_text("Üye ekle", wait_for="Ad soyad")
    check("ekip formu doldurulabiliyor",
          fill({"Ad soyad": "Deniz Yılmaz", "Ünvan": "Kurucu Ortak",
                "E-posta": "deniz@dalga0.test"}) == "ok",
          str(fill({"Ad soyad": "Deniz Yılmaz"})))
    # Uyarı forma ait: gönderimden sonra form kapanıyor, önce kontrol edilir.
    check("KVKK uyarısı formda geçiyor",
          form_text and "kişisel veridir" in form_text, (form_text or "")[:400])
    text = browser.click_text("Kaydet", wait_for="ekibe eklendi")
    check("ekip üyesi eklendi mesajı", text and "ekibe eklendi" in text,
          (text or "")[:300])
    team = api(f"/api/startups/{new_id}", tokens["kulucka"])["team"]
    check("üye sunucuda kayıtlı", any(m["fullName"] == "Deniz Yılmaz" for m in team),
          json.dumps([m["fullName"] for m in team]))

    # --- Kapsam dışı program: sunucu reddediyor --------------------------
    print("\n=== Kapsam dışı program ===")
    if foreign_term:
        status, body = post("/api/participations", tokens["kulucka"], {
            "startupId": new_id, "programTermId": foreign_term["id"],
            "status": "Accepted", "joinedOn": "2026-03-01",
            "leftOn": None, "notes": None})
        check("kapsam dışı programa bağlama reddediliyor", status == 403, str(status))
        check("hata mesajı gerekçeyi söylüyor",
              "sorumluluğunuzda değil" in json.dumps(body, ensure_ascii=False),
              json.dumps(body, ensure_ascii=False)[:300])
    else:
        print("  atla  kapsam dışı dönem bulunamadı")

    # --- Rol ayrımı: girişim kullanıcısı hâlâ yalnızca öneriyor ----------
    print("\n=== Girişim kullanıcısı yalnızca öneri gönderiyor ===")
    text = as_user("portal", "/portal", wait_for="Girişim profili")
    check("portalda öneri düğmesi var", "Değişikliği öner" in text, text[:400])
    check("portalda doğrudan kaydetme yok", "Kaydet" not in text, text[:400])
    check("portalda 'Yeni üye öner' düğmesi var", "Yeni üye öner" in text, text[:400])
    check("portalda 'Üye ekle' düğmesi yok", "Üye ekle" not in text, text[:400])

    text = as_user("portal", f"/girisimler/{portal_startup}", wait_for="Toplam yatırım")
    check("girişim kullanıcısı kendi kartında 'Düzenle' görmüyor",
          "Düzenle" not in text, text[:400])
    check("girişim kullanıcısı 'Kaydı pasife al' görmüyor",
          "pasife al" not in text, text[:400])

    text = as_user("karar", "/girisimler", wait_for="Girişimler")
    check("karar vericide 'Yeni girişim' düğmesi yok", "Yeni girişim" not in text,
          text[:400])

    # --- B-04: hız sınırı bölümlemesi ------------------------------------
    # En sonda: bu blok admin hesabının kovasını bir dakika boyunca doldurur.
    print("\n=== Giriş hız sınırı bölümlemesi ===")
    codes = [login("admin@t3ekosistem.test", "yanlis-sifre")[0] for _ in range(11)]
    check("yanlış şifre denemeleri sonunda 429 geliyor", 429 in codes, str(codes))
    other, _ = login("karar.verici@t3ekosistem.test")
    check("başka hesap aynı anda giriş yapabiliyor (kova bölümlendi)", other == 200,
          str(other))
    blocked, _ = login("admin@t3ekosistem.test")
    check("kilitlenen hesap doğru şifreyle de beklemek zorunda", blocked == 429,
          str(blocked))
finally:
    browser.close()

print(f"\n{ok} kontrol geçti, {len(failures)} başarısız")
for failure in failures:
    print(f"  - {failure}")
sys.exit(1 if failures else 0)
