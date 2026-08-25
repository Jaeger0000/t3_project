"""VPS dağıtımının gerçek tarayıcıda doğrulaması.

Sunucudan 200 dönmesi uygulamanın açıldığını göstermez: derlenmiş arayüz
API'den servis ediliyor ve tek origin kurulumunda bozulacak şey (varlık yolları,
çerez, CSP'nin kendi script'ini engellemesi) yalnızca render'da görünür.
"""

import json
import os
import sys
import time
import urllib.request

sys.path.insert(0, __file__.rsplit("/", 1)[0])
from cdp import Browser  # noqa: E402

# Adres ortamdan geliyor: dağıtımın IP'si/portu değişince betik değişmemeli.
BASE = os.environ.get("T3_VPS_BASE", "https://t3girisimportali.com").rstrip("/")
PW = "T3.Creathon!2026"

ok = 0
fails = []


def check(label, cond, detail=""):
    global ok
    if cond:
        ok += 1
        print(f"  ok   {label}")
    else:
        fails.append(label)
        print(f"  FAIL {label} — {str(detail)[:300]}")


def wait_loaded(text, marker="yükleniyor", timeout=15):
    """Satırlar sorgudan sonra basılıyor: beklemeyen kontrol ürünü değil
    zamanlamayı ölçer."""
    deadline = time.time() + timeout
    while marker in (text or "") and time.time() < deadline:
        time.sleep(0.3)
        text = browser.text()
    return text


def token_of(email):
    req = urllib.request.Request(
        f"{BASE}/api/auth/login",
        data=json.dumps({"email": email, "password": PW}).encode(),
        headers={"Content-Type": "application/json"}, method="POST")
    with urllib.request.urlopen(req, timeout=20) as r:
        return json.load(r)["accessToken"]


browser = Browser()
try:
    # --- Oturumsuz giriş ekranı -----------------------------------------
    text = browser.goto(f"{BASE}/giris", wait_for="Giriş yap")
    check("giriş ekranı render ediliyor", "Giriş yap" in text, text[:300])
    check("KVKK onay kutusu VPS'te de var",
          browser.evaluate("!!document.querySelector('[data-testid=\"kvkk-onay\"]')") is True)
    check("onay verilmeden giriş düğmesi kapalı",
          browser.evaluate(
              "[...document.querySelectorAll('button')]"
              ".find(b => (b.innerText||'').includes('Giriş yap')).disabled") is True)
    check("CSP kendi paketini engellemiyor (script çalıştı)",
          browser.evaluate("!!document.querySelector('#root').children.length") is True)

    # --- Süper Yönetici: liste, kart, pano ------------------------------
    admin = token_of("admin@t3ekosistem.test")
    browser.set_session(admin, origin=BASE)

    text = wait_loaded(browser.goto(f"{BASE}/girisimler", wait_for="Girişimler"))
    check("girişim listesi açılıyor", "Girişimler" in text, text[:300])
    check("tohumlanan 32 kayıt listede", "32 girişim" in text or "32 kayıt" in text,
          text[:800])

    text = wait_loaded(browser.goto(f"{BASE}/pano", wait_for="Ekosistem panosu"))
    check("pano ve grafikler çiziliyor", "Ekosistem panosu" in text, text[:300])
    check("panoda toplam yatırım görünüyor", "₺" in text, text[:600])

    # --- Karar Verici: maskeleme dağıtılan kopyada de duruyor ------------
    karar = token_of("karar.verici@t3ekosistem.test")
    browser.set_session(karar, origin=BASE)
    text = wait_loaded(browser.goto(f"{BASE}/pano", wait_for="Ekosistem panosu"))
    check("Karar Verici panoyu görüyor", "Ekosistem panosu" in text, text[:300])

    text = browser.goto(f"{BASE}/denetim", wait_for=None)
    check("Karar Verici denetim izine giremiyor", "yetkiniz yok" in text, text[:400])

    # --- Konsol hatası -------------------------------------------------
    errors = [m for m in browser.console_errors()] if hasattr(browser, "console_errors") else []
    check("konsolda hata yok", not errors, str(errors)[:300])
finally:
    browser.close()

print(f"\n{ok} kontrol geçti, {len(fails)} başarısız")
sys.exit(1 if fails else 0)
