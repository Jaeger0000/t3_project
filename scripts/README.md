# Doğrulama betikleri

Birim testleri (`dotnet test`) iş kurallarını, buradaki betikler **çalışan
sistemi** doğrular. Üçü birlikte Faz 2'den beri uygulanan sırayı oluşturuyor:
`dotnet test` → API'ye tüm rollerle vuran uçtan uca betik → gerçek tarayıcıda
render. Sonuncusu olmadan "index.html 200 döndü" ile "uygulama açıldı"
karıştırılıyor; Faz 2'deki `0 ₺` maskeleme hatasını yalnızca render adımı
yakalamıştı.

| Betik | Ne doğrular |
|---|---|
| `e2e_faz3.py` | Onay akışı, kuyruk kapsamı, diff maskelemesi, denetim izi, kullanıcı yönetimi ve soft delete zinciri — API'ye altı rolle gerçek istek atarak (81 kontrol) |
| `e2e_faz4.py` | Başarı/finans kayıtları ve dokümanlar: tür-alan doğrulaması, tutar maskelemesi, yükleme kuralları, indirme yetkisi, onay/ret sonrası dosya yaşam döngüsü (123 kontrol) |
| `render_faz3.py` | Faz 3 ekranlarının headless Chrome'da render olması, rol bazlı menü ve maskelemenin **ekranda** doğrulanması (32 kontrol) |
| `render_faz4.py` | Başarı ve doküman sekmeleri, portalın öneri yolu, yeni hedef türlerinin diff ekranı — tutarın Karar Verici'de ekranda **olmadığı** ve portaldan formla gönderilen önerinin kuyruğa düştüğü dâhil (41 kontrol) |
| `e2e_faz5.py` | Ekosistem karnesi, karnede kapsam ve agregat/satır maskeleme ayrımı, CSV dışa aktarma, AI karar destek uçları, MCP sunucusu (JSON-RPC) ve MCP–REST sayı tutarlılığı (103 kontrol) |
| `render_faz6.py` | Denetim Dalga 0 düzeltmeleri: girişim/ekip/katılım yazma yolları (arayüzden yeni girişim → program dönemine bağlama → kart düzenleme → ekip üyesi), sekme başlığı ve sayfa dili, 404 ekranı, üç genişlikte mobil yatay taşma, kapsam dışı program reddi, maskeli alanın yazma yolunda korunması ve giriş hız sınırı bölümlemesi (75 kontrol) |
| `render_faz5.py` | Pano ve grafiklerin çizilmesi, Karar Verici'nin ekosistem toplamını görüp tekil tutar yerine kilit görmesi, tarayıcıdan CSV indirme, asistan paneli ve kart özeti — kaynak listesiyle birlikte (53 kontrol) |
| `cdp.py` | Render betiklerinin kullandığı bağımlısız Chrome DevTools Protocol istemcisi |

## Çalıştırma

Uçtan uca betikler veriyi **değiştirir** (öneri onaylar, kayıt siler, girişim
pasife alır). Her biri temiz tohum verisi bekler, bu yüzden **her uçtan uca
betikten önce veritabanı sıfırlanır**. Render betikleri veriyi değiştirmez;
`render_faz3.py` denetim izi satırlarını `e2e_faz3.py`'den bekler, bu yüzden
onunla aynı turda çalışır.

```bash
# --- Tur 1: Faz 3 ---
docker compose down -v && docker compose up -d postgres
cd backend && set -a && . ../.env && set +a
dotnet ef database update \
  --project "$PWD/src/T3.Infrastructure/T3.Infrastructure.csproj" \
  --startup-project "$PWD/src/T3.Infrastructure/T3.Infrastructure.csproj"
ASPNETCORE_URLS=http://localhost:5080 dotnet run --project src/T3.Api/T3.Api.csproj &

python3 scripts/e2e_faz3.py
python3 scripts/render_faz3.py     # ayrıca `cd frontend && npm run dev` gerekir

# --- Tur 2: Faz 4 (veritabanını yeniden sıfırlayın) ---
python3 scripts/e2e_faz4.py
python3 scripts/render_faz4.py

# --- Tur 3: Faz 5 (veritabanını yeniden sıfırlayın) ---
python3 scripts/e2e_faz5.py
python3 scripts/render_faz5.py

# --- Tur 4: Dalga 0 (aynı veritabanının üstünde çalışır, veriyi değiştirir) ---
python3 scripts/render_faz6.py
```

`render_faz6.py` en sona bırakılır: yeni bir girişim oluşturup program dönemine
bağlıyor, kart alanlarını değiştiriyor ve sonunda `admin@` hesabının giriş
kovasını bilinçli olarak dolduruyor (hız sınırı bölümlemesi kontrolü). Aynı
turda ondan sonra çalışan bir betik 429 alır.

`render_faz5.py` beklenen sayıları API'den okur (girişim sayısı, toplam yatırım,
CSV satır sayısı), bu yüzden tohum verisi büyüdüğünde kendiliğinden güncel kalır.
Sıfırlanmamış bir veritabanında ise düşer: önceki turlar girişim pasife alıp
kayıt sildiği için kapsam sayıları değişir.

> **Uyarı — API sürecini önce durdurun.** Sıfırlama sırasında eski API ayakta
> kalırsa yeni süreç 5080'e bağlanamaz ve istekler bozuk bağlantı havuzuna
> sahip eski sürece gider. Kalıp kendi kabuğunu öldürmesin diye
> `pkill -f 'T3[.]Api'` **ayrı** bir komut olarak çalıştırılır.
>
> Giriş ucunda dakikada 10 istek sınırı var ve betikler altı hesapla giriş
> yapıyor. İki koşu arasında 429 alırsanız API sürecini yeniden başlatın —
> sayaç bellekte tutuluyor.
>
> `e2e_faz4.py` depoya yazılan dosyaları da doğruladığı için
> `backend/src/T3.Api/storage/` klasörünü okur; sıfırlarken o klasör de silinir.
>
> Python dışında bağımlılık yok; render betikleri yalnızca
> `google-chrome-stable` bekler.
