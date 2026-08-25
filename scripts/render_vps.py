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


# Türkçe büyük/küçük dönüşümü karşılaştırma için güvenli değil: "TÜRKİYE".lower()
# birleşik noktalı bir "i̇" üretiyor, "TAKIMI".lower() ise "takimi" veriyor. Sunucu
# tarafında aynı iş `SearchText.Fold` ile yapılıyor; betik de aynı katlamayı
# kullanıyor ki kontroller CSS'in büyük harfe çevirdiği başlıklarda da çalışsın.
_KATLAMA = str.maketrans({
    "İ": "i", "I": "i", "ı": "i", "Ş": "s", "ş": "s", "Ğ": "g", "ğ": "g",
    "Ü": "u", "ü": "u", "Ö": "o", "ö": "o", "Ç": "c", "ç": "c",
})


def katla(metin):
    return (metin or "").translate(_KATLAMA).lower()


def bekle_metin(beklenen, timeout=25):
    """Satırlar sorgudan sonra basılıyor; beklemeden yapılan kontrol ürünü değil
    zamanlamayı ölçer.

    Beklemeyi "yükleniyor" metnine bağlamak kırılgan çıktı: her ekranın kendi
    ifadesi var (pano "Karne hesaplanıyor…" diyor) ve eşleşmeyen bir markör
    beklemeyi sessizce atlıyor. Bu yüzden beklenen **içeriğin kendisi**
    bekleniyor: gelmezse kontrol zaten düşmeli."""
    deadline = time.time() + timeout
    text = browser.text()
    while beklenen not in (text or "") and time.time() < deadline:
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
    # Logo satır içi SVG: harici dosya olsaydı CSP img-src/connect-src tarafında
    # ayrı bir karar isterdi ve bir istek daha ederdi.
    check("logo ekranda çizilmiş",
          browser.evaluate(
              "(() => { const s = document.querySelector('[data-testid=\"logo\"]');"
              "  return !!s && s.getBoundingClientRect().width > 8; })()") is True)

    # --- Süper Yönetici: liste, kart, pano ------------------------------
    admin = token_of("admin@t3ekosistem.test")
    browser.set_session(admin, origin=BASE)

    browser.goto(f"{BASE}/girisimler", wait_for="Girişimler")
    text = bekle_metin("32 girişim")
    check("girişim listesi açılıyor", "Girişimler" in text, text[:300])
    check("tohumlanan 32 kayıt listede", "32 girişim" in text, text[:800])

    browser.goto(f"{BASE}/pano", wait_for="Ekosistem panosu")
    text = bekle_metin("₺")
    check("pano ve grafikler çiziliyor", "Ekosistem panosu" in text, text[:300])
    check("panoda toplam yatırım görünüyor", "₺" in text, text[:600])

    # --- Karar Verici: maskeleme dağıtılan kopyada de duruyor ------------
    karar = token_of("karar.verici@t3ekosistem.test")
    browser.set_session(karar, origin=BASE)
    browser.goto(f"{BASE}/pano", wait_for="Ekosistem panosu")
    text = bekle_metin("Ekosistem panosu")
    check("Karar Verici panoyu görüyor", "Ekosistem panosu" in text, text[:300])

    text = browser.goto(f"{BASE}/denetim", wait_for=None)
    check("Karar Verici denetim izine giremiyor", "yetkiniz yok" in text, text[:400])

    # --- Marka / logo paketi sayfası ------------------------------------
    # Oturumsuz açılmalı: sponsor ve partnerler de doğru kullanımı görecek.
    browser.clear_session()
    # Başlıklar CSS ile büyük harfe çevriliyor ve innerText bu dönüşümü
    # uyguluyor: beklenen metin gövde cümlesinden alınıyor, karşılaştırmalar
    # harf duyarsız (aynı tuzak render_faz5'te de yazılı).
    text = browser.goto(f"{BASE}/marka", wait_for="TGM işareti dört renkli")
    katli = katla(text)
    check("logo paketi oturumsuz açılıyor",
          "turkiye teknoloji takimi vakfi" in katli and "ana marka isareti" in katli,
          text[:300])
    check("dört marka rengi de ekranda",
          all(h in katli for h in ("#303c48", "#e43c24", "#f99b1c", "#0c4878")), text[:600])
    check("Pantone ve CMYK değerleri var",
          "7545 c" in katli and "0 74 84 11" in katli, text[:600])
    # Yazı tipleri kendi sunucumuzdan geliyor; CSP font-src 'self' yüzünden
    # harici bir kaynaktan gelseydi sessizce sistem yazı tipine düşerdi.
    browser.evaluate("document.fonts.ready")
    time.sleep(0.6)
    check("Barlow Condensed yüklendi",
          browser.evaluate("document.fonts.check('600 26px \"Barlow Condensed\"')") is True,
          browser.evaluate("[...document.fonts].map(f => f.family + ' ' + f.status).join(', ')"))
    check("TGM işareti gerçekten indirildi (kırpılmış 333 px kaynak)",
          browser.evaluate(
              "(() => { const i = [...document.images].find(x => x.src.includes('tgm-logo'));"
              "  return i ? i.naturalWidth : 0; })()") in (333, 447))
    check("blueprint köşe işaretleri duruyor",
          (browser.evaluate("document.querySelectorAll('.absolute.size-\\[11px\\]').length") or 0) >= 16
          or (browser.evaluate("document.querySelectorAll('i[aria-hidden=\"true\"]').length") or 0) >= 16)

    # Renk kopyalama: ipucu metni değişiyor (pano izni headless'ta yoksa da
    # görsel geri bildirim gösterilmeli — kontrol tam olarak bunu ölçüyor).
    browser.evaluate("document.querySelector('[data-testid=\"swatch-E43C24\"]').click()")
    time.sleep(0.4)
    check("renk alanına tıklayınca kopyalandı bildirimi çıkıyor",
          "kopyalandı" in (browser.evaluate(
              "document.querySelector('[data-testid=\"kopya-ipucu\"]').textContent") or ""))

    # Zemin seçici: üç mod logo dosyasını, açıklamayı ve kuralları birlikte
    # değiştiriyor.
    browser.evaluate("document.querySelector('[data-testid=\"zemin-2\"]').click()")
    time.sleep(0.4)
    check("fotoğraf modunda tek renk beyaz sürüm kullanılıyor",
          "beyaz" in (browser.evaluate(
              "document.querySelector('[data-testid=\"zemin-logosu\"]').getAttribute('src')") or ""))
    check("fotoğraf modunun kuralları ekranda",
          "koyu örtü" in (browser.evaluate(
              "document.querySelector('[data-testid=\"zemin-aciklama\"]').parentElement.textContent") or ""))

    # --- Konsol hatası -------------------------------------------------
    errors = [m for m in browser.console_errors()] if hasattr(browser, "console_errors") else []
    check("konsolda hata yok", not errors, str(errors)[:300])
finally:
    browser.close()

print(f"\n{ok} kontrol geçti, {len(fails)} başarısız")
sys.exit(1 if fails else 0)
