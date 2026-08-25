"""Dalga 2 düzeltmelerinin gerçek tarayıcıda doğrulaması.

Kapsam: üretim dağıtım yolu (B-07), jetonun çereze taşınması + CSP (Y-05) ve
kalan orta öncelikli maddeler (O-01 URL durumu, O-04 aksan katlaması,
O-05 kirli form uyarısı, O-06 kod bölme, D-01 erişilebilirlik).

Betik **iki origin'e** karşı koşuyor ve ayrım kasıtlı:

  * http://localhost:5173 — Vite geliştirme sunucusu. Davranış kontrolleri
    burada, çünkü geliştirici günlük olarak bu ekranı görüyor.
  * http://localhost:5080 — API'nin kendisi. Derlenmiş arayüzü buradan sunuyor;
    "npm run build çıktısı hiçbir API'ye ulaşamıyor" bulgusunun kapandığı tek
    yer bu. Güvenlik başlıkları, SPA geri dönüşü ve kod bölme burada sınanıyor.

Ön koşul: `npm run build` çıktısı backend/src/T3.Api/wwwroot içine kopyalanmış
ve API yeniden başlatılmış olmalı (bkz. scripts/README.md).

Betik veri **değiştirmiyor**: yalnızca okur, filtreler ve bir formu doldurup
kaydetmeden bırakır.
"""

import json
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

sys.path.insert(0, __file__.rsplit("/", 1)[0])
from cdp import Browser  # noqa: E402

DEV = "http://localhost:5173"
PROD = "http://localhost:5080"
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


def call(method, path, token=None, body=None, headers=None):
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(API + path, data=data, method=method)
    if data is not None:
        req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    for name, value in (headers or {}).items():
        req.add_header(name, value)
    try:
        with urllib.request.urlopen(req) as r:
            raw = r.read().decode(errors="replace")
            return r.status, dict(r.headers), (json.loads(raw) if raw.strip() else None)
    except urllib.error.HTTPError as e:
        raw = e.read().decode(errors="replace")
        try:
            return e.code, dict(e.headers), json.loads(raw)
        except json.JSONDecodeError:
            return e.code, dict(e.headers), {"raw": raw}


def token_of(email, password=PW):
    status, _, body = call("POST", "/api/auth/login", None,
                           {"email": email, "password": password})
    if status != 200:
        raise SystemExit(f"{email} giriş yapamadı: {status} {body}")
    return body["accessToken"]


def raw_login_cookies():
    """Girişteki tüm Set-Cookie satırlarını ham okur.

    `dict(headers)` yetmiyor: aynı adı taşıyan başlıklardan yalnızca sonuncusu
    kalıyor ve giriş iki çerez yazıyor.
    """
    data = json.dumps({"email": "admin@t3ekosistem.test", "password": PW}).encode()
    req = urllib.request.Request(API + "/api/auth/login", data=data, method="POST")
    req.add_header("Content-Type", "application/json")
    with urllib.request.urlopen(req) as r:
        return r.headers.get_all("Set-Cookie") or [], json.loads(r.read().decode())


def raw_get(url, headers=None):
    """Tarayıcı dışında ham istek: başlıkları ve gövdeyi olduğu gibi görmek için."""
    req = urllib.request.Request(url, method="GET")
    for name, value in (headers or {}).items():
        req.add_header(name, value)
    try:
        with urllib.request.urlopen(req) as r:
            return r.status, dict(r.headers), r.read().decode(errors="replace")
    except urllib.error.HTTPError as e:
        return e.code, dict(e.headers), e.read().decode(errors="replace")


admin = token_of("admin@t3ekosistem.test")
browser = Browser()


def as_admin(origin, path, wait_for=None):
    browser.goto(f"{origin}/giris")
    browser.set_session(admin, origin=origin)
    return browser.goto(f"{origin}{path}", wait_for=wait_for)


try:
    # --- Y-05: jeton çerezde, localStorage'da değil ----------------------
    print("\n=== Oturum çerezi (Y-05) ===")
    cookie_lines, body = raw_login_cookies()
    all_cookies = " | ".join(cookie_lines)
    check("giriş oturum çerezi yazıyor", "t3.session=" in all_cookies, all_cookies[:300])
    check("oturum çerezi HttpOnly", "httponly" in all_cookies.lower(), all_cookies[:300])
    check("oturum çerezi SameSite=Strict", "samesite=strict" in all_cookies.lower(),
          all_cookies[:300])
    check("CSRF çerezi ayrıca yazılıyor (okunabilir olmalı)",
          "t3.csrf=" in all_cookies, all_cookies[:300])
    check("jeton gövdede de dönüyor (betik/MCP yolu duruyor)",
          bool(body.get("accessToken")), json.dumps(body)[:120])

    text = as_admin(DEV, "/pano", wait_for="Ekosistem panosu")
    check("çerezle açılan oturum panoyu getiriyor", "Ekosistem panosu" in text, text[:200])
    check("script oturum çerezini okuyamıyor",
          "t3.session" not in (browser.evaluate("document.cookie") or ""),
          browser.evaluate("document.cookie"))
    keys = browser.evaluate("Object.keys(localStorage).join(',')") or ""
    check("localStorage'da jeton yok", "accessToken" not in keys, keys)
    check("localStorage yalnızca oturum işaretini tutuyor",
          keys in ("", "t3.session.active"), keys)

    # --- Y-05: CSRF çift gönderimi --------------------------------------
    print("\n=== CSRF koruması (Y-05) ===")
    # Çerezle kimliklenen yazma isteği başlık olmadan reddedilmeli. Tarayıcı
    # içinden ölçülüyor: çerezi ekleyen taraf tarayıcının kendisi.
    result = browser.evaluate("""
      (async () => {
        const res = await fetch('/api/startups', {
          method: 'POST', credentials: 'same-origin',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ name: 'CSRF denemesi', sector: 'Other', status: 'Active' }),
        });
        return res.status;
      })()""")
    check("CSRF başlığı olmayan yazma isteği reddediliyor", result == 403, str(result))

    result = browser.evaluate("""
      (async () => {
        const csrf = document.cookie.match(/t3\\.csrf=([^;]*)/);
        const res = await fetch('/api/startups/00000000-0000-0000-0000-000000000000', {
          method: 'PUT', credentials: 'same-origin',
          headers: {
            'Content-Type': 'application/json',
            'X-CSRF-Token': csrf ? decodeURIComponent(csrf[1]) : '',
          },
          body: JSON.stringify({ name: 'Yok', sector: 'Other', status: 'Active' }),
        });
        return res.status;
      })()""")
    # 404: CSRF kapısını geçti, kayıt bulunamadı. Aranan şey kapının geçilmesi.
    check("CSRF başlığı taşıyan istek kapıyı geçiyor", result == 404, str(result))

    status, _, _ = call("PUT", "/api/startups/00000000-0000-0000-0000-000000000000",
                        admin, {"name": "Yok", "sector": "Other", "status": "Active"})
    check("Authorization başlığıyla gelen istek CSRF istemiyor", status == 404, str(status))

    # --- Y-05: çıkış çerezi siliyor -------------------------------------
    print("\n=== Çıkış (Y-05) ===")
    text = as_admin(DEV, "/pano", wait_for="Ekosistem panosu")
    text = browser.click_text("Çıkış", wait_for="posta") or browser.text()
    cookies = [c["name"] for c in browser.call("Network.getCookies")["cookies"]]
    check("çıkış oturum çerezini siliyor", "t3.session" not in cookies, str(cookies))
    check("çıkıştan sonra giriş ekranı", "posta" in text.lower(), text[:200])
    check("çıkışta 'süre doldu' gerekçesi gösterilmiyor",
          "süresi doldu" not in text, text[:300])

    # --- Y-01: süresi dolmuş/geçersiz oturum -----------------------------
    # Jeton çereze taşınınca oturumu düşürme işi `queryClient.clear()`'a
    # bırakılmıştı ve **çalışmıyordu**: React Query hata durumunda eldeki
    # `data`'yı koruyor, yani 401 alan kullanıcı ekranda oturumda kalıyordu.
    print("\n=== Süresi dolmuş oturum (Y-01) ===")
    browser.goto(f"{DEV}/giris")
    browser.set_session("gecersiz.jeton.degeri", origin=DEV)
    text = browser.goto(f"{DEV}/pano", wait_for="posta")
    check("geçersiz jeton giriş ekranına düşürüyor", "posta" in text.lower(), text[:200])
    check("gerekçe ekranda yazıyor", "süresi doldu" in text.lower(), text[:400])
    check("panoda kalmıyor", browser.evaluate("location.pathname") == "/giris",
          browser.evaluate("location.pathname"))

    # --- B-07: üretim yolu (arayüzü API sunuyor) -------------------------
    print("\n=== Üretim dağıtım yolu (B-07) ===")
    status, headers, html = raw_get(f"{PROD}/")
    check("API kök adreste arayüzü sunuyor", status == 200, str(status))
    check("dönen içerik HTML", "text/html" in headers.get("Content-Type", ""),
          headers.get("Content-Type", ""))
    check("index.html uygulama kökünü taşıyor", 'id="root"' in html, html[:200])

    status, _, deep = raw_get(f"{PROD}/girisimler/00000000-0000-0000-0000-000000000000")
    check("istemci rotası index.html'e düşüyor (SPA geri dönüşü)",
          status == 200 and 'id="root"' in deep, str(status))

    status, headers, body_text = raw_get(f"{PROD}/api/olmayan-uc")
    check("bilinmeyen API yolu index.html değil JSON 404 döner",
          status == 404 and "application/json" in headers.get("Content-Type", ""),
          f"{status} {headers.get('Content-Type')}")
    check("404 gövdesi Türkçe API hatası",
          "İstenen kaynak bulunamadı" in body_text, body_text[:200])

    text = as_admin(PROD, "/girisimler", wait_for="Girişimler")
    check("derlenmiş arayüz tek origin'de giriş yapabiliyor",
          "Girişimler" in text, text[:200])
    check("tek origin'de liste veriyle doluyor",
          any(ch.isdigit() for ch in text) and "Sunucuya ulaşılamıyor" not in text,
          text[:300])

    # --- Y-05: güvenlik başlıkları --------------------------------------
    print("\n=== Güvenlik başlıkları (Y-05) ===")
    _, headers, _ = raw_get(f"{PROD}/")
    csp = headers.get("Content-Security-Policy", "")
    check("CSP başlığı var", bool(csp), str(headers)[:300])
    check("CSP script-src 'self' ile sınırlı",
          "script-src 'self'" in csp and "unsafe-eval" not in csp, csp)
    check("CSP çerçeveye gömmeyi kapatıyor", "frame-ancestors 'none'" in csp, csp)
    check("nosniff başlığı var",
          headers.get("X-Content-Type-Options") == "nosniff", str(headers)[:200])
    check("referrer politikası var", bool(headers.get("Referrer-Policy")), str(headers)[:200])

    _, api_headers, _ = raw_get(f"{PROD}/api/startups?pageSize=1",
                                {"Authorization": "Bearer " + admin})
    check("API yanıtı önbelleğe girmiyor (no-store)",
          "no-store" in api_headers.get("Cache-Control", ""),
          api_headers.get("Cache-Control", ""))

    # Statik varlıklar tersine: adında içerik özeti var, sonsuza kadar önbellek.
    asset = ""
    for line in html.split("\n"):
        if "/assets/" in line and ".js" in line:
            asset = line.split('src="')[-1].split('"')[0] if 'src="' in line else ""
            break
    if asset:
        _, asset_headers, _ = raw_get(PROD + asset)
        check("özet adlı varlık uzun süre önbelleklenebiliyor",
              "immutable" in asset_headers.get("Cache-Control", ""),
              asset_headers.get("Cache-Control", ""))

    # --- O-06: rota bazlı kod bölme -------------------------------------
    print("\n=== Kod bölme (O-06) ===")
    as_admin(PROD, "/pano", wait_for="Ekosistem panosu")
    loaded = browser.evaluate(
        "performance.getEntriesByType('resource').map(e => e.name).join('\\n')") or ""
    check("pano kendi parçasını indiriyor", "DashboardPage" in loaded, loaded[-400:])
    check("girişim kartı parçası pano açılışında indirilmiyor",
          "StartupDetailPage" not in loaded, loaded[-400:])
    browser.goto(f"{PROD}/girisimler", wait_for="Girişimler")
    loaded = browser.evaluate(
        "performance.getEntriesByType('resource').map(e => e.name).join('\\n')") or ""
    check("liste ekranı gezinince ayrı parça olarak iniyor",
          "StartupsPage" in loaded, loaded[-400:])

    # --- O-01: filtre/sıralama/sayfa URL'de ------------------------------
    print("\n=== Filtre durumu URL'de (O-01) ===")
    as_admin(DEV, "/girisimler", wait_for="Girişimler")
    browser.evaluate("""
      (() => {
        const label = [...document.querySelectorAll('label')].find(
          (l) => (l.innerText || '').trim().startsWith('Sektör'));
        const el = label.querySelector('select');
        Object.getOwnPropertyDescriptor(window.HTMLSelectElement.prototype, 'value')
          .set.call(el, 'Health');
        el.dispatchEvent(new Event('change', { bubbles: true }));
      })()""")
    time.sleep(1.2)
    check("sektör filtresi adres çubuğuna yazılıyor",
          "sektor=Health" in (browser.evaluate("location.search") or ""),
          browser.evaluate("location.search"))

    filtered = browser.text()
    reloaded = browser.goto(f"{DEV}/girisimler?sektor=Health", wait_for="Girişimler")
    check("paylaşılan bağlantı aynı listeyi açıyor",
          browser.evaluate(
              "[...document.querySelectorAll('label')]"
              ".find(l => l.innerText.trim().startsWith('Sektör'))"
              ".querySelector('select').value") == "Health",
          reloaded[:200])

    browser.evaluate("history.back()")
    time.sleep(1.5)
    check("geri tuşu filtreyi kaldırıyor",
          "sektor=Health" not in (browser.evaluate("location.search") or ""),
          browser.evaluate("location.search"))
    check("filtreli liste boş kalmadı (kontrol veriye dayanıyor)",
          "Girişimler" in filtered, filtered[:200])

    # --- O-04: aksan katlaması ------------------------------------------
    print("\n=== Aksan katlaması (O-04) ===")
    # Terim gerçek veriden türetiliyor: sabit yazılsa tohum verisi değişince
    # kontrol sessizce anlamsızlaşırdı.
    _, _, page = call("GET", "/api/startups?pageSize=100", admin)
    accented = next(
        (s["name"] for s in page["items"]
         if any(ch in s["name"] for ch in "çğışöüÇĞİŞÖÜ")), None)
    if accented:
        folded = (accented.lower()
                  .replace("ç", "c").replace("ğ", "g").replace("ı", "i")
                  .replace("ö", "o").replace("ş", "s").replace("ü", "u")
                  .replace("i̇", "i"))
        _, _, found = call(
            "GET", "/api/startups?q=" + urllib.parse.quote(folded), admin)
        names = [s["name"] for s in found["items"]]
        check(f"aksansız yazım aksanlı kaydı buluyor ({folded} → {accented})",
              accented in names, json.dumps(names, ensure_ascii=False)[:300])

        _, _, exact = call(
            "GET", "/api/startups?q=" + urllib.parse.quote(accented), admin)
        check("aksanlı yazım da aynı kaydı buluyor",
              accented in [s["name"] for s in exact["items"]],
              json.dumps([s["name"] for s in exact["items"]], ensure_ascii=False)[:200])
    else:
        check("aksanlı girişim adı bulundu (kontrolün ön koşulu)", False, "veri yok")

    # --- O-05: kirli formdan çıkışta uyarı -------------------------------
    print("\n=== Kirli form uyarısı (O-05) ===")
    as_admin(DEV, "/girisimler", wait_for="Girişimler")
    browser.click_text("Yeni girişim", wait_for="Yeni girişim")
    browser.evaluate("""
      (() => {
        const label = [...document.querySelectorAll('label')].find(
          (l) => (l.innerText || '').trim().startsWith('Girişim adı'));
        const el = label.querySelector('input');
        Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value')
          .set.call(el, 'Kaydedilmemiş taslak');
        el.dispatchEvent(new Event('input', { bubbles: true }));
      })()""")
    time.sleep(0.6)
    text = browser.text()
    # Uyarı yazmadan önce değil, ayrılmaya çalışırken çıkıyor: form doldurulurken
    # kullanıcıya "dikkat" demek gürültü olurdu.
    check("form doldurulurken uyarı gösterilmiyor",
          "Kaydedilmemiş değişiklikler var" not in text, text[:200])

    browser.evaluate("""
      (() => {
        const link = [...document.querySelectorAll('a')].find(
          (a) => (a.innerText || '').trim() === 'Pano');
        link.click();
      })()""")
    time.sleep(1.0)
    text = browser.text()
    check("kaydedilmemiş değişiklik uyarısı ayrılırken çıkıyor",
          "Kaydedilmemiş değişiklikler var" in text, text[:400])
    check("uygulama içi gezinme engellendi",
          browser.evaluate("location.pathname") == "/girisimler",
          browser.evaluate("location.pathname"))
    text = browser.click_text("Ayrıl, kaydetmeden", wait_for="Ekosistem panosu")
    check("kullanıcı onaylayınca gezinme sürüyor",
          browser.evaluate("location.pathname") == "/pano",
          browser.evaluate("location.pathname"))

    # --- O-05: sekmeler arası oturum senkronu ---------------------------
    print("\n=== Sekmeler arası senkron (O-05) ===")
    as_admin(DEV, "/pano", wait_for="Ekosistem panosu")
    # Başka sekmede çıkış yapılmış gibi: çerez gider, işaret değişir.
    browser.call("Network.deleteCookies", name="t3.session", url=DEV)
    browser.evaluate("""
      localStorage.removeItem('t3.session.active');
      window.dispatchEvent(new StorageEvent('storage', { key: 't3.session.active' }));
    """)
    deadline = time.time() + 15
    text = ""
    while time.time() < deadline:
        time.sleep(0.4)
        text = browser.text()
        if "posta" in text.lower():
            break
    check("diğer sekmedeki çıkış bu sekmeyi de giriş ekranına alıyor",
          "posta" in text.lower(), text[:300])

    # --- D-01: erişilebilirlik ------------------------------------------
    print("\n=== Erişilebilirlik (D-01) ===")
    as_admin(DEV, "/pano", wait_for="Ekosistem panosu")
    skip = browser.evaluate("""
      (() => {
        const link = document.querySelector('a[href="#icerik"]');
        if (!link) return 'yok';
        const before = getComputedStyle(link).clipPath;
        link.focus();
        const after = getComputedStyle(link);
        return JSON.stringify({
          text: link.innerText.trim(),
          target: !!document.getElementById('icerik'),
          hiddenByDefault: before === 'inset(50%)',
          visibleOnFocus: after.clipPath === 'none'
            && link.getBoundingClientRect().height > 10,
        });
      })()""")
    check("içeriğe atla bağlantısı var", skip != "yok", str(skip))
    if skip != "yok":
        info = json.loads(skip)
        check("bağlantı metni Türkçe ve hedefi mevcut",
              info["text"] == "İçeriğe atla" and info["target"], skip)
        check("normalde ekranda görünmüyor", info["hiddenByDefault"], skip)
        check("odaklanınca görünür oluyor", info["visibleOnFocus"], skip)

    # Odak halkası: Tailwind halkayı box-shadow olarak üretiyor, rengin alfası
    # %20'den %60'a çıkarıldı (klavye kullanıcısı odağı görebilmeli).
    browser.goto(f"{DEV}/girisimler", wait_for="Girişimler")
    ring = browser.evaluate("""
      (() => {
        const label = [...document.querySelectorAll('label')].find(
          (l) => (l.innerText || '').trim().startsWith('Ara'));
        const input = label && label.querySelector('input');
        if (!input) return 'girdi yok';
        input.focus();
        const style = getComputedStyle(input);
        return JSON.stringify({
          shadow: style.boxShadow,
          ring: style.getPropertyValue('--tw-ring-color').trim(),
        });
      })()""")
    check("odak halkası ölçülebildi", ring != "girdi yok", str(ring))
    if ring != "girdi yok":
        info = json.loads(ring)
        blob = (info["shadow"] or "") + " " + (info["ring"] or "")
        check("odak halkası çiziliyor",
              info["shadow"] not in ("", "none"), info["shadow"])
        check("halka rengi %60 opaklıkta (eskiden %20)",
              "0.6" in blob or "60%" in blob, blob)

finally:
    browser.close()

print(f"\n{ok} kontrol geçti, {len(failures)} başarısız")
for failure in failures:
    print(f"  - {failure}")
sys.exit(1 if failures else 0)
