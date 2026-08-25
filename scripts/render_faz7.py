"""Dalga 1 düzeltmelerinin gerçek tarayıcıda ve gerçek API'ye karşı doğrulaması.

Denetim planının Dalga 1 maddeleri (program/dönem yönetimi B-02, şifre kurtarma
B-03, oturum ömrü ve ağ hatası Y-01, giriş olaylarının denetim izi B-05, KVKK
metinleri B-06, Karar Verici'nin onay ekranı O-03) mevcut betiklerin hiçbirinin
kapsamında değil: ekranların çoğu bu dalgada ilk kez var oldu.

Betik veriyi **değiştirir**: geçici bir program + dönem + katılım oluşturup
yaşam döngüsünü tamamlayıp temizler, tek kullanımlık bir kullanıcı açar ve o
kullanıcının şifresini sıfırlar. Tohum hesaplarının şifresine dokunmaz —
diğer betikler onlarla giriş yapıyor.

Sıra bilinçli: hız sınırı kontrolü en sonda, çünkü kullandığı kovayı bir dakika
boyunca dolduruyor.
"""

import glob
import json
import os
import re
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
OUTBOX = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    "backend", "src", "T3.Api", "storage", "outbox")

STAMP = int(time.time()) % 100000
NEW_PROGRAM = f"Dalga1 Test Programı {STAMP}"
NEW_TERM = f"{STAMP} Dönemi"
TEST_USER = f"dalga1.test{STAMP}@t3ekosistem.test"
TEST_PW = "Gecici.Sifre1"
NEW_PW = "Kalici.Sifre9"
RESET_PW = "Sifirlanan.Sifre7"

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


def call(method, path, token=None, body=None):
    """(durum kodu, gövde) döner; hata gövdesi de ayrıştırılır."""
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(API + path, data=data, method=method)
    if data is not None:
        req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req) as r:
            raw = r.read().decode(errors="replace")
            return r.status, (json.loads(raw) if raw.strip() else None)
    except urllib.error.HTTPError as e:
        raw = e.read().decode(errors="replace")
        try:
            return e.code, json.loads(raw)
        except json.JSONDecodeError:
            return e.code, {"raw": raw}


def api(path, token=None):
    status, body = call("GET", path, token)
    if status != 200:
        raise SystemExit(f"GET {path} → {status} {body}")
    return body


def login(email, password=PW):
    return call("POST", "/api/auth/login", None, {"email": email, "password": password})


def token_of(email, password=PW):
    status, body = login(email, password)
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

# Aynı etiketli düğme ekranda birden fazla kez var (her program kartında bir
# "Dönem ekle"). Bu yüzden tıklama, ilgili kaydın adını taşıyan öğeden yukarı
# yürüyüp o kartın içindeki düğmeyi buluyor.
CLICK_IN_CARD_JS = """
(() => {
  const wanted = __NAME__;
  const label = __LABEL__;
  const anchor = [...document.querySelectorAll('h1, h2, h3, h4, p, span')]
    .find((n) => (n.innerText || '').trim() === wanted);
  if (!anchor) return 'kart bulunamadı';
  let node = anchor;
  while (node && node !== document.body) {
    const button = [...node.querySelectorAll('button, a')]
      .find((b) => (b.innerText || '').trim().startsWith(label));
    if (button) { button.click(); return 'ok'; }
    node = node.parentElement;
  }
  return 'düğme bulunamadı';
})()
"""


def fill(fields):
    return browser.evaluate(FILL_JS.replace("__FIELDS__", json.dumps(fields)))


def click_tab(label, wait_for=None, timeout=10):
    """Sekmeye tıklar. Yalnızca <button> arıyor: üst menüdeki "Programlar"
    bağlantısı aynı metni taşıyor ve belge sırasında önce geliyor — genel
    tıklayıcı sekme yerine menüyü tıklayıp betiği sessizce yanlış sayfaya
    götürüyordu."""
    clicked = browser.evaluate(
        "(() => {"
        "  const wanted = " + json.dumps(label) + ";"
        "  const el = [...document.querySelectorAll('button')]"
        "    .find(n => (n.innerText || '').trim().startsWith(wanted));"
        "  if (!el) return false;"
        "  el.click();"
        "  return true;"
        "})()")
    if not clicked:
        return None
    deadline = time.time() + timeout
    while time.time() < deadline:
        time.sleep(0.3)
        text = browser.text()
        if wait_for is None or wait_for in text:
            return text
    return browser.text()


def click_in_card(name, label, wait_for=None, timeout=10):
    result = browser.evaluate(
        CLICK_IN_CARD_JS
        .replace("__NAME__", json.dumps(name))
        .replace("__LABEL__", json.dumps(label)))
    if result != "ok":
        return result
    deadline = time.time() + timeout
    while time.time() < deadline:
        time.sleep(0.3)
        text = browser.text()
        if wait_for is None or wait_for in text:
            return text
    return browser.text()


def as_user(role_or_token, path, wait_for=None):
    token = tokens.get(role_or_token, role_or_token)
    browser.goto(f"{APP}/giris")
    browser.set_session(token)
    return browser.goto(f"{APP}{path}", wait_for=wait_for)


def anonymous(path, wait_for=None):
    browser.goto(f"{APP}/giris")
    browser.clear_session()
    return browser.goto(f"{APP}{path}", wait_for=wait_for)


def _hours_ahead(iso):
    """ISO 8601 damgasının kaç saat sonrasını gösterdiği.

    .NET, saniyenin kesrini yedi haneli yazıyor; Python'un ayrıştırıcısı üç ya
    da altı hane bekliyor, bu yüzden kesir baştan kırpılıyor."""
    from datetime import datetime, timezone

    value = re.sub(r"\.\d+", "", iso).replace("Z", "+00:00")
    return (datetime.fromisoformat(value) - datetime.now(timezone.utc)).total_seconds() / 3600


def newest_outbox_mail():
    """Geliştirme kutusundaki en son e-posta. Sıfırlama jetonu HTTP yanıtında
    hiç dönmüyor; doğrulama da bu yüzden postayı okuyor."""
    files = sorted(glob.glob(os.path.join(OUTBOX, "*.txt")))
    return open(files[-1], encoding="utf-8").read() if files else ""


try:
    # --- B-06: KVKK metinleri --------------------------------------------
    print("\n=== KVKK metinleri ve alt bilgi ===")
    text = anonymous("/kvkk-aydinlatma", wait_for="aydınlatma")
    check("aydınlatma metni giriş yapmadan açılıyor", "Veri sorumlusu" in text, text[:300])
    check("işlenen veri kategorileri sayılıyor", "İşlenen kişisel veriler" in text, text[:300])
    check("KVKK m.11 hakları ve başvuru adresi var",
          "m.11" in text and "kvkk@t3vakfi.org.tr" in text, text[:600])
    check("metnin taslak olduğu söyleniyor", "Taslak" in text, text[:300])
    check("adres çubuğu korunuyor",
          browser.evaluate("location.pathname") == "/kvkk-aydinlatma",
          browser.evaluate("location.pathname"))

    text = anonymous("/kullanim-sartlari", wait_for="Kullanım şartları")
    check("kullanım şartları giriş yapmadan açılıyor", "Hesap güvenliği" in text, text[:300])
    check("yapay zekânın karar destek olduğu yazıyor",
          "karar" in text and "destek" in text, text[:400])

    text = anonymous("/giris", wait_for="Giriş yap")
    check("giriş ekranından KVKK metnine bağlantı var",
          "KVKK aydınlatma metni" in text, text[:400])
    check("giriş ekranında 'Şifremi unuttum' var", "Şifremi unuttum" in text, text[:400])

    text = as_user("admin", "/pano", wait_for="Ekosistem panosu")
    check("alt bilgi her ekranda KVKK bağlantısı taşıyor",
          "KVKK aydınlatma metni" in text and "Kullanım şartları" in text, text[-400:])
    check("alt bilgide başvuru/destek yolu var",
          "Destek ve KVKK başvurusu" in text, text[-400:])

    # --- O-03: Karar Verici'nin onay ekranı ------------------------------
    print("\n=== Karar Verici'nin onay ekranı ===")
    text = as_user("karar", "/onaylar", wait_for="yetkiniz yok")
    check("karar verici gerekçeyi görüyor",
          "Bu ekranı görme yetkiniz yok" in text, text[:400])
    check("sonsuza dek boş 'Önerilerim' ekranı gösterilmiyor",
          "Önerilerim" not in text, text[:400])

    text = as_user("portal", "/onaylar", wait_for="Öneri")
    check("girişim kullanıcısı kendi önerilerini görmeye devam ediyor",
          "yetkiniz yok" not in text, text[:400])
    text = as_user("kulucka", "/onaylar", wait_for="Onay")
    check("program yöneticisinin kuyruğu değişmedi", "yetkiniz yok" not in text, text[:400])

    # --- Y-01: oturum ömrü, derin bağlantı, ağ hatası --------------------
    print("\n=== Oturum ömrü ve ağ hatası ===")
    status, body = login("admin@t3ekosistem.test")
    expires = body["expiresAt"]
    check("jeton ömrü iş günü kadar (15 dakika değil)",
          expires and expires[:2] == "20" and _hours_ahead(expires) >= 6,
          f"{expires}")

    # Derin bağlantı: oturumsuz kullanıcı giriş sonrası gitmek istediği ekrana döner.
    browser.goto(f"{APP}/giris")
    browser.clear_session()
    browser.goto(f"{APP}/denetim", wait_for="Giriş yap")
    check("oturumsuz derin bağlantı giriş ekranına düşüyor",
          browser.evaluate("location.pathname") == "/giris",
          browser.evaluate("location.pathname"))
    check("giriş formu doldurulabiliyor",
          fill({"E-posta": "admin@t3ekosistem.test", "Şifre": PW}) == "ok")
    browser.click_text("Giriş yap", wait_for="Denetim izi")
    check("giriş sonrası istenen ekrana dönülüyor",
          browser.evaluate("location.pathname") == "/denetim",
          browser.evaluate("location.pathname"))

    # Ağ hatası: jeton elde kalıyor, ham "Failed to fetch" yerine Türkçe durum.
    as_user("admin", "/pano", wait_for="Ekosistem panosu")
    browser.call("Network.enable")
    # Yalnızca API engelleniyor: tüm ağı kesmek uygulamanın kendisini de
    # indirilemez yapar ve Chrome'un hata sayfası çıkar — sınanmak istenen ise
    # uygulamanın açık olup sunucuya ulaşamaması.
    browser.call("Network.setBlockedURLs", urls=["*/api/*"])
    browser.call("Page.reload")
    deadline = time.time() + 20
    text = ""
    while time.time() < deadline:
        time.sleep(0.4)
        text = browser.text()
        if "ulaşılamıyor" in text or "Yeniden dene" in text:
            break
    check("ağ kesildiğinde Türkçe durum gösteriliyor", "ulaşılamıyor" in text, text[:300])
    check("ham 'Failed to fetch' ekranda yok", "Failed to fetch" not in text, text[:300])
    check("yeniden deneme düğmesi var", "Yeniden dene" in text, text[:300])
    # Jeton HttpOnly çerezde: script okuyamıyor, çerez deposuna CDP ile bakıyoruz.
    cookies = [c["name"] for c in browser.call("Network.getCookies")["cookies"]]
    check("ağ hatası oturumu düşürmüyor (oturum çerezi duruyor)",
          "t3.session" in cookies, str(cookies))
    check("giriş ekranına atılmıyor", browser.evaluate("location.pathname") == "/pano",
          browser.evaluate("location.pathname"))

    browser.call("Network.setBlockedURLs", urls=[])
    text = browser.click_text("Yeniden dene", wait_for="Ekosistem panosu")
    check("bağlantı gelince aynı ekran açılıyor", text and "Ekosistem panosu" in text,
          (text or "")[:300])

    # --- B-02: program ve dönem yönetimi --------------------------------
    print("\n=== Program ve dönem yönetimi ===")
    text = as_user("admin", "/programlar", wait_for="Programlar")
    check("süper yöneticide 'Yeni program' düğmesi var", "Yeni program" in text, text[:400])

    text = as_user("kulucka", "/programlar", wait_for="Programlar")
    check("program yöneticisi program tanımlayamıyor", "Yeni program" not in text, text[:400])
    check("program yöneticisi dönem ekleyebiliyor", "Dönem ekle" in text, text[:600])

    text = as_user("karar", "/programlar", wait_for="Programlar")
    check("karar verici hiçbir yazma düğmesi görmüyor",
          "Yeni program" not in text and "Dönem ekle" not in text, text[:600])

    status, body = call("POST", "/api/programs", tokens["kulucka"],
                        {"name": "Olmayacak Program", "type": "Incubation",
                         "coordinatorship": None, "description": None})
    check("program yöneticisi API'den de program oluşturamıyor", status == 403, str(status))

    as_user("admin", "/programlar", wait_for="Programlar")
    browser.click_text("Yeni program", wait_for="Program adı")
    check("program formu doldurulabiliyor",
          fill({"Program adı": NEW_PROGRAM, "Tür": "Acceleration",
                "Koordinatörlük": "Girişim Merkezi",
                "Açıklama": "Dalga 1 doğrulaması"}) == "ok")
    text = browser.click_text("Programı oluştur", wait_for="oluşturuldu")
    check("program oluşturuldu mesajı", text and "oluşturuldu" in text, (text or "")[:300])
    programs = api("/api/programs", tokens["admin"])
    created = next((p for p in programs if p["name"] == NEW_PROGRAM), None)
    check("program sunucuda kayıtlı", created is not None,
          json.dumps([p["name"] for p in programs])[:300])

    if created:
        result = click_in_card(NEW_PROGRAM, "Dönem ekle", wait_for="Dönem adı")
        check("kart içinden dönem formu açılıyor",
              isinstance(result, str) and "Dönem adı" in result, str(result)[:200])
        check("dönem formu doldurulabiliyor",
              fill({"Dönem adı": NEW_TERM, "Başlangıç": "2026-09-01"}) == "ok")
        text = browser.click_text("Dönemi ekle", wait_for="eklendi")
        check("dönem eklendi mesajı", text and "eklendi" in text, (text or "")[:300])

        created = next(p for p in api("/api/programs", tokens["admin"])
                       if p["id"] == created["id"])
        term = next((t for t in created["terms"] if t["name"] == NEW_TERM), None)
        check("dönem sunucuda kayıtlı", term is not None,
              json.dumps([t["name"] for t in created["terms"]]))

        # Katılımın yaşam döngüsü: ekle (API) → düzelt (arayüz) → kaldır (arayüz).
        if term:
            status, participation = call("POST", "/api/participations", tokens["admin"], {
                "startupId": portal_startup, "programTermId": term["id"],
                "status": "Accepted", "joinedOn": "2026-09-01",
                "leftOn": None, "notes": "Dalga 1 doğrulaması"})
            check("geçici katılım eklendi", status == 201, f"{status} {participation}")

            text = as_user("admin", f"/girisimler/{portal_startup}", wait_for="Toplam yatırım")
            text = click_tab("Programlar", wait_for="Program katılımları")
            check("katılım listesi açılıyor",
                  text and "Program katılımları" in text and NEW_PROGRAM in text,
                  (text or "")[:400])
            result = click_in_card(NEW_PROGRAM, "Katılımı düzelt", wait_for="Durum")
            check("katılım düzeltme formu açılıyor",
                  isinstance(result, str) and "Katılımı kaldır" in result, str(result)[:300])
            check("durum değiştirilebiliyor",
                  fill({"Durum": "Graduated", "Not": "Mezun oldu"}) == "ok")
            text = browser.click_text("Kaydet", wait_for="güncellendi")
            check("katılım güncellendi mesajı", text and "güncellendi" in text,
                  (text or "")[:300])
            card = api(f"/api/startups/{portal_startup}", tokens["admin"])
            row = next((p for p in card["programs"]
                        if p["programName"] == NEW_PROGRAM), None)
            check("katılım durumu sunucuda güncellendi",
                  row is not None and row["status"] == "Graduated", json.dumps(row))

            # Dönemde katılım varken kapatma reddediliyor: gelişim yolculuğunun
            # kaynağı tek tıkla boşaltılamamalı.
            status, body = call("DELETE",
                                f"/api/programs/{created['id']}/terms/{term['id']}",
                                tokens["admin"])
            check("katılımı olan dönem kapatılamıyor", status == 409, str(status))
            check("hata gerekçeyi söylüyor",
                  "katılımları kaldırın" in json.dumps(body, ensure_ascii=False),
                  json.dumps(body, ensure_ascii=False)[:300])

            result = click_in_card(NEW_PROGRAM, "Katılımı düzelt", wait_for="Katılımı kaldır")
            browser.click_text("Katılımı kaldır", wait_for="Emin misiniz")
            text = browser.click_text("Evet, kaldır", wait_for="kaldırıldı")
            check("katılım arayüzden kaldırılabiliyor", text and "kaldırıldı" in text,
                  (text or "")[:300])
            card = api(f"/api/startups/{portal_startup}", tokens["admin"])
            check("katılım sunucudan da düştü",
                  all(p["programName"] != NEW_PROGRAM for p in card["programs"]),
                  json.dumps([p["programName"] for p in card["programs"]]))

            as_user("admin", "/programlar", wait_for="Programlar")
            click_in_card(NEW_TERM, "Kapat", wait_for="Evet, kapat")
            text = click_in_card(NEW_TERM, "Evet, kapat", wait_for="kapatıldı")
            check("boş dönem arayüzden kapatılabiliyor",
                  isinstance(text, str) and "kapatıldı" in text, str(text)[:300])
            after = next(p for p in api("/api/programs", tokens["admin"])
                         if p["id"] == created["id"])
            check("dönem listeden düştü",
                  all(t["name"] != NEW_TERM for t in after["terms"]),
                  json.dumps([t["name"] for t in after["terms"]]))

        # Planın kabul kriteri: yeni program bir yöneticiye kapsam olarak
        # atanabilmeli ve o yönetici programı görmeli.
        pm = next(u for u in api("/api/users?pageSize=100", tokens["admin"])["items"]
                  if u["email"] == "kulucka.yoneticisi@t3ekosistem.test")
        status, _ = call("PUT", f"/api/users/{pm['id']}", tokens["admin"], {
            "fullName": pm["fullName"], "role": pm["role"], "startupId": None,
            "programIds": [p["id"] for p in pm["programs"]] + [created["id"]],
            "isActive": True})
        check("yeni program yöneticiye kapsam olarak atanabiliyor", status == 200, str(status))

        # Kapsam jetonun claim'inde taşınıyor: mevcut oturum yeni programı
        # görmüyor, yönetici yeniden giriş yapmak zorunda. Demo sırasında
        # şaşırtabilecek bir davranış, bu yüzden burada açıkça sınanıyor.
        stale = [p["name"] for p in api("/api/programs", tokens["kulucka"])]
        check("eski jeton yeni kapsamı görmüyor (claim'de taşınıyor)",
              NEW_PROGRAM not in stale, json.dumps(stale, ensure_ascii=False)[:200])
        fresh_pm = token_of("kulucka.yoneticisi@t3ekosistem.test")
        names = [p["name"] for p in api("/api/programs", fresh_pm)]
        check("yönetici yeniden girişte yeni programı kapsamında görüyor",
              NEW_PROGRAM in names, json.dumps(names, ensure_ascii=False)[:300])

        status, body = call("DELETE", f"/api/programs/{created['id']}", tokens["admin"])
        check("program kapatılabiliyor", status == 200, f"{status} {body}")
        check("program kapanınca yönetici ataması da düşüyor",
              isinstance(body, dict) and body.get("managerAssignments", 0) >= 1,
              json.dumps(body, ensure_ascii=False)[:200])
        check("kapatma zinciri raporlanıyor",
              isinstance(body, dict) and "terms" in body and "managerAssignments" in body,
              json.dumps(body, ensure_ascii=False)[:300])
        check("kapatılan program listeden düştü",
              all(p["name"] != NEW_PROGRAM for p in api("/api/programs", tokens["admin"])),
              NEW_PROGRAM)

    # --- B-03: şifre kurtarma ve zorunlu değiştirme ---------------------
    print("\n=== Şifre kurtarma ===")
    status, new_user = call("POST", "/api/users", tokens["admin"], {
        "email": TEST_USER, "fullName": f"Dalga1 Test {STAMP}",
        "role": "DecisionMaker", "password": TEST_PW,
        "startupId": None, "programIds": []})
    check("tek kullanımlık test hesabı oluşturuldu", status == 201, f"{status} {new_user}")

    if status == 201:
        test_token = token_of(TEST_USER, TEST_PW)
        me = api("/api/me", test_token)
        check("yönetici atadığı şifre geçici sayılıyor",
              me["mustChangePassword"] is True, json.dumps(me)[:200])

        text = as_user(test_token, "/pano", wait_for="Şifre")
        check("zorunlu değiştirme başka ekrana geçirmiyor",
              browser.evaluate("location.pathname") == "/sifre-degistir",
              browser.evaluate("location.pathname"))
        check("gerekçe ekranda yazıyor",
              "yönetici tarafından atandı" in text, text[:400])

        check("şifre değiştirme formu doldurulabiliyor",
              fill({"Mevcut şifre": TEST_PW, "Yeni şifre": NEW_PW,
                    "Yeni şifre (yeniden)": NEW_PW}) == "ok")
        text = browser.click_text("Şifreyi güncelle", wait_for="güncellendi")
        check("şifre arayüzden değiştirilebiliyor", text and "güncellendi" in text,
              (text or "")[:300])

        status, body = login(TEST_USER, NEW_PW)
        check("yeni şifreyle giriş yapılabiliyor", status == 200, f"{status} {body}")
        if status == 200:
            check("zorunlu değiştirme bayrağı düştü",
                  body["user"]["mustChangePassword"] is False, json.dumps(body["user"])[:200])
            fresh = body["accessToken"]

            status, _ = call("POST", "/api/auth/change-password", fresh,
                             {"currentPassword": "Yanlis.Sifre1", "newPassword": "Baska.Sifre8"})
            check("mevcut şifre yanlışsa değiştirme reddediliyor", status == 400, str(status))

        # Sıfırlama: yanıt adresin kayıtlı olup olmadığını söylemiyor.
        status, unknown = call("POST", "/api/auth/forgot-password", None,
                               {"email": f"olmayan{STAMP}@t3ekosistem.test"})
        check("olmayan adres için de 200 dönüyor", status == 200, f"{status} {unknown}")
        status, known = call("POST", "/api/auth/forgot-password", None, {"email": TEST_USER})
        check("kayıtlı adres için de aynı yanıt",
              status == 200 and known["message"] == unknown["message"],
              json.dumps([known, unknown], ensure_ascii=False)[:300])

        mail = newest_outbox_mail()
        check("sıfırlama bağlantısı e-postaya yazıldı",
              TEST_USER in mail and "/sifre-sifirla/" in mail, mail[:300])
        reset_token = mail.split("/sifre-sifirla/")[-1].split()[0] if "/sifre-sifirla/" in mail else ""
        check("jeton HTTP yanıtında dönmüyor",
              reset_token and reset_token not in json.dumps(known), reset_token[:20])

        if reset_token:
            token_value = urllib.parse.unquote(reset_token)
            status, body = call("POST", "/api/auth/reset-password", None,
                                {"token": token_value, "newPassword": "kisa"})
            check("zayıf şifre sıfırlamada da reddediliyor", status == 400, str(status))

            status, body = call("POST", "/api/auth/reset-password", None,
                                {"token": token_value, "newPassword": RESET_PW})
            check("jetonla şifre sıfırlanıyor", status == 200, f"{status} {body}")
            check("yanıt ham e-posta taşımıyor",
                  status != 200 or TEST_USER not in json.dumps(body),
                  json.dumps(body, ensure_ascii=False)[:200])

            status, body = call("POST", "/api/auth/reset-password", None,
                                {"token": token_value, "newPassword": "Ucuncu.Sifre3"})
            check("jeton ikinci kullanımda reddediliyor", status == 400, str(status))

            status, body = login(TEST_USER, RESET_PW)
            check("sıfırlanan şifreyle giriş yapılabiliyor", status == 200, f"{status} {body}")

        text = anonymous("/sifre-sifirla/gecersiz-jeton", wait_for="Yeni şifre")
        check("sıfırlama ekranı geçersiz jetonla da açılıyor",
              "Yeni şifre belirle" in text, text[:300])
        check("ekran jetonu göstermiyor", "gecersiz-jeton" not in text, text[:300])

        text = anonymous("/sifremi-unuttum", wait_for="Şifre sıfırlama")
        check("şifremi unuttum ekranı açılıyor", "Sıfırlama bağlantısı gönder" in text,
              text[:300])

    # --- B-05: giriş olayları denetim izinde ----------------------------
    print("\n=== Giriş olayları denetim izinde ===")
    login("admin@t3ekosistem.test", "Bu.Sifre.Yanlis9")
    logs = api("/api/audit-logs?action=Auth&pageSize=100", tokens["admin"])
    actions = {row["action"] for row in logs["items"]}
    check("başarılı giriş ize düşüyor", "Auth.LoginSucceeded" in actions, str(sorted(actions)))
    check("başarısız giriş ize düşüyor", "Auth.LoginFailed" in actions, str(sorted(actions)))
    check("şifre işlemleri ize düşüyor",
          {"Auth.PasswordChanged", "Auth.PasswordReset",
           "Auth.PasswordResetRequested"} <= actions, str(sorted(actions)))

    failed = [r for r in logs["items"] if r["action"] == "Auth.LoginFailed"]
    check("başarısız denemede IP kaydediliyor",
          bool(failed) and bool(failed[0]["ipAddress"]), json.dumps(failed[:1])[:300])
    check("istemci bilgisi kaydediliyor",
          bool(failed) and bool(failed[0]["userAgent"]), json.dumps(failed[:1])[:300])
    check("e-posta maskeli saklanıyor",
          bool(failed) and "***" in (failed[0]["afterJson"] or ""),
          json.dumps(failed[:1])[:300])
    raw_dump = json.dumps(logs, ensure_ascii=False)
    check("ham e-posta ize yazılmıyor",
          "admin@t3ekosistem.test" not in raw_dump.replace('"email"', ""),
          raw_dump[:300])

    status, _ = call("GET", "/api/audit-logs?action=Auth", tokens["portal"])
    check("girişim kullanıcısı giriş izini göremiyor", status == 403, str(status))

    text = as_user("admin", "/denetim", wait_for="Denetim izi")
    check("izde eylem türü süzgeci var", "Başarısız giriş" in text, text[:600])
    check("kimliği doğrulanmamış olay ayırt ediliyor",
          "kimlik doğrulanmadı" in text, text[:1200])

    # --- B-04 tamamlayıcı: hız sınırı kilidi de ize düşüyor -------------
    print("\n=== Hız sınırı kilidi (kova bir dakika dolu kalır) ===")
    before = api("/api/audit-logs?action=Auth.RateLimited&pageSize=100",
                 tokens["admin"])["totalCount"]
    locked = f"kilit{STAMP}@t3ekosistem.test"
    codes = [login(locked, "Yanlis.Sifre1")[0] for _ in range(14)]
    check("yeterli denemeden sonra 429 geliyor", 429 in codes, str(codes))
    after = api("/api/audit-logs?action=Auth.RateLimited&pageSize=100",
                tokens["admin"])["totalCount"]
    check("kilit ize düşüyor", after > before, f"{before} → {after}")
    check("her reddedilen istek için satır açılmıyor (pencerede bir kayıt)",
          after - before == 1, f"{before} → {after}, {codes.count(429)} adet 429")

    # --- Temizlik: tek kullanımlık hesap listede kalmasın ---------------
    # e2e_faz3 kullanıcı sayısını sabit bekliyor; bu betiğin bıraktığı hesap
    # oradaki kontrolü sessizce düşürüyordu. Silme soft delete: kayıt duruyor,
    # listeden ve girişten çıkıyor.
    print("\n=== Temizlik ===")
    users = api("/api/users?pageSize=100", tokens["admin"])["items"]
    temp = next((u for u in users if u["email"] == TEST_USER), None)
    if temp:
        status, _ = call("DELETE", f"/api/users/{temp['id']}", tokens["admin"])
        check("tek kullanımlık hesap pasife alındı", status in (200, 204), str(status))

finally:
    browser.close()

print(f"\n{ok} kontrol geçti, {len(failures)} başarısız")
for failure in failures:
    print(f"  - {failure}")

sys.exit(1 if failures else 0)
