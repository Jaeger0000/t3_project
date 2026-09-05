# Ajanlar

Bu klasördeki dosyalar projeye özel **alt ajanlardır** (subagent). Her biri dar
bir işi, projenin kendi kurallarıyla yapar; ana oturumun bağlamını şişirmeden
çalışır. Ajan seçimi otomatiktir (`description` alanına göre) ama adıyla da
çağrılabilir.

| Ajan | Ne yapar | Kod yazar mı |
|---|---|---|
| `urun-denetcisi` | Ürünü kullanıcı gözüyle denetler; rol × rota matrisini yürür, kanıtlı bulgu raporu ve **canlıya çıkış kararı** verir | ❌ |
| `rbac-kvkk-denetcisi` | Kapsam filtresi, alan maskelemesi, yazma yolu koruması ve altı sızıntı yolunu (JSON, karne, CSV, diff, AI, MCP) denetler | ❌ |
| `kod-gozden-gecirici` | Değişikliği mimari kurallara karşı okur: katman yönü, dikey dilim, MediatR/mapper yasağı, doğrulama yeri, sağlayıcı bağımsız EF, Türkçe/kültür | ❌ |
| `backend-dilim` | Backend'e use-case ekler/değiştirir: Domain → Application → Infrastructure → Api → test → migration | ✅ |
| `arayuz-dilim` | Frontend ekran/özellik yazar: rota, form, sorgu, maskeleme gösterimi, erişilebilirlik, `npm run build` | ✅ |
| `dogrulama-kosucusu` | Üç katmanlı doğrulama zincirini doğru sırayla koşar; düşen kontrolü ürün hatası / veri / zamanlama / ortam diye ayırır | ❌ |
| `demo-hazirlik` | Sunum, demo senaryosu, kanvas ve jüri sorusu hazırlığı — hepsi çalışan üründeki gerçek ekranlara dayanır | ✅ (yalnız `docs/`) |

## Tipik zincirler

**Yeni özellik:** `backend-dilim` → `arayuz-dilim` → `dogrulama-kosucusu` →
`kod-gozden-gecirici`

**Yetkiyi etkileyen değişiklik:** `backend-dilim` → `rbac-kvkk-denetcisi` →
`dogrulama-kosucusu`

**Teslim öncesi:** `dogrulama-kosucusu` → `urun-denetcisi` →
`rbac-kvkk-denetcisi` → `demo-hazirlik`

## Ortak zemin

Ajanların hepsi aynı belgelere dayanır; kural değiştiğinde ajan dosyası değil
**önce o belge** güncellenir:

- `CLAUDE.md` — her görevde geçerli kurallar (kısa tutulur)
- `docs/Problem7_T3_Girisim_Ekosistemi_Proje_Brifi.md` — zorunlu altı MVP maddesi
- `docs/Problem7_Teknik_Plan.md` — mimari, veri modeli, yetki matrisi, API yüzeyi
- `docs/Gelistirme_Kararlari.md` — yerleşik kararlar, reddedilen alternatifler, tuzaklar
- `README.md` — kurulum, demo hesapları, komutlar, bilinen açık işler ve **bilinçli sınırlar**
- `scripts/README.md` — doğrulama betikleri ve çalıştırma sırası

`urun-denetcisi`, `docs/Urun_Denetcisi_Ajan_Promptu.md` içindeki promptun
çalıştırılabilir halidir. O belge gerekçeyi ve tam metni tutar; ajan dosyası
oturumda yüklenen sürümdür. Denetim ölçütü değişirse **ikisi birlikte** güncellenir.

## Yeni ajan eklerken

- Dosya adı = `name` alanı = kebab-case, Türkçe.
- `description` alanı ajanın **ne zaman seçileceğini** söyler; ne yaptığını değil.
  Otomatik seçim bu alana bakar.
- `tools` alanı dar tutulur: denetçi ajanlar `Write`/`Edit` almaz — teşhis koyar,
  düzeltmeyi geliştirici ajana bırakır.
- Kuralı ajan dosyasına kopyalamak yerine kaynağına **atıf ver**; kopyalanan
  kural eskir.
- Türkçe yaz.
