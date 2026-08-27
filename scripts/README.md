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
| `e2e_faz4.py` | Başarı/finans kayıtları ve dokümanlar: tür-alan doğrulaması, tutar maskelemesi, yükleme kuralları, indirme yetkisi, onay/ret sonrası dosya yaşam döngüsü (125 kontrol) |
| `render_faz3.py` | Faz 3 ekranlarının headless Chrome'da render olması, rol bazlı menü ve maskelemenin **ekranda** doğrulanması (32 kontrol) |
| `render_faz4.py` | Başarı ve doküman sekmeleri, portalın öneri yolu, yeni hedef türlerinin diff ekranı — tutarın Karar Verici'de ekranda **olmadığı** ve portaldan formla gönderilen önerinin kuyruğa düştüğü dâhil (41 kontrol) |
| `e2e_faz5.py` | Ekosistem karnesi, karnede kapsam ve agregat/satır maskeleme ayrımı, CSV dışa aktarma, AI karar destek uçları, MCP sunucusu (JSON-RPC) ve MCP–REST sayı tutarlılığı (104 kontrol) |
| `render_faz6.py` | Denetim Dalga 0 düzeltmeleri: girişim/ekip/katılım yazma yolları (arayüzden yeni girişim → program dönemine bağlama → kart düzenleme → ekip üyesi), sekme başlığı ve sayfa dili, 404 ekranı, üç genişlikte mobil yatay taşma, kapsam dışı program reddi, maskeli alanın yazma yolunda korunması ve giriş hız sınırı bölümlemesi (75 kontrol) |
| `render_faz5.py` | Pano ve grafiklerin çizilmesi, Karar Verici'nin ekosistem toplamını görüp tekil tutar yerine kilit görmesi, tarayıcıdan CSV indirme, asistan paneli ve kart özeti — kaynak listesiyle birlikte (53 kontrol) |
| `render_faz8.py` | Denetim Dalga 2: oturum çerezinin bayrakları ve script'in jetona erişemediği, CSRF çift-gönderimi (çerezle gelen yazma isteği reddediliyor, başlıkla gelen istemiyor), çıkışın çerezi silmesi, **derlenmiş arayüzün API'den sunulması** (SPA geri dönüşü + API yolunda JSON 404), güvenlik başlıkları ve önbellek politikası, rota bazlı kod bölme, filtre durumunun URL'de olması, aksansız aramanın aksanlı kaydı bulması, kaydedilmemiş form uyarısı, sekmeler arası oturum senkronu, "İçeriğe atla" ve odak halkası, süresi dolmuş oturumun giriş ekranına düşmesi (54 kontrol) |
| `render_faz7.py` | Denetim Dalga 1: program/dönem yönetimi ve katılımın yaşam döngüsü (oluştur → düzelt → kaldır → dönemi kapat → programı kapat), şifre kurtarmanın tamamı (zorunlu değiştirme, kutudan okunan jeton, tek kullanımlık kontrolü), oturum ömrü ve **ağ kesintisinde** oturumun düşmemesi, giriş olaylarının denetim izine maskeli düşmesi, KVKK metinlerinin oturumsuz açılması, **giriş ekranındaki KVKK onay kapısı** (kutu işaretlenmeden giriş düğmesi kapalı, onay denetim izine maskeli düşüyor), Karar Verici'nin onay ekranı (97 kontrol) |
| `render_vps.py` | Canlı VPS dağıtımının gerçek tarayıcıda doğrulaması: giriş ekranı ve KVKK onay kapısı, CSP'nin kendi paketini engellemediği, logonun çizilmesi, listenin/panonun veriyle çizilmesi, Karar Verici'nin denetim izine girememesi, sıfır konsol hatası. Adres `T3_VPS_BASE` ile verilir (12 kontrol) |
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

# --- Tur 4: Dalga 0 + Dalga 1 + Dalga 2 (aynı veritabanı, veriyi değiştirir) ---
python3 scripts/render_faz6.py
sleep 65                          # faz6'nın doldurduğu giriş kovası boşalsın
python3 scripts/render_faz7.py
sleep 65
python3 scripts/render_faz8.py
```

Hacmi silmek mümkün değilse (yetki yok, docker köprüsü bozuk) **tur başına ayrı
veritabanı** aynı işi yapıyor: `CREATE DATABASE t3_tur1` → bağlantı dizesindeki
`Database=` adını çevir → `dotnet ef database update` → API'yi başlat. Geliştirme
ortamında tohumlayıcı boş şemayı doldurduğu için sonuç temiz tohum verisiyle
aynı, üstelik önceki turun verisi incelenmek üzere yerinde kalıyor.

`render_faz8.py` **iki origin'e** karşı koşuyor ve bu yüzden ek bir hazırlık
istiyor: davranış kontrolleri Vite'ta (5173), barındırma ve güvenlik başlıkları
API'nin kendi sunduğu derlenmiş arayüzde (5080). Öncesinde:

```bash
cd frontend && npm run build
rm -rf ../backend/src/T3.Api/wwwroot && cp -r dist ../backend/src/T3.Api/wwwroot
# API yeniden başlatılır (wwwroot açılışta okunuyor)
```

Jetonun çereze taşınmasıyla render betiklerinin oturum kurma biçimi de değişti:
`localStorage`'a jeton yazmak artık hiçbir şey yapmıyor (kapanan açığın kendisi
bu). `cdp.py` çerezi CDP ile yazıyor (`Browser.set_session`); CSRF çerezi de
yazılmak zorunda, yoksa her yazma isteği 403 alır.

`render_faz6.py` ve `render_faz7.py` en sona bırakılır: ikisi de veri
oluşturuyor ve sonlarında giriş kovasını bilinçli olarak dolduruyor (hız sınırı
kontrolleri). Aralarında bir dakika beklenmezse `render_faz7.py` giriş
aşamasında 429 alır.

`render_faz7.py` kendi verisini temizler: açtığı program, dönem ve katılımı
yaşam döngüsünün sonunda kapatır. Tohum hesaplarının şifresine dokunmaz —
şifre akışlarını tek kullanımlık bir hesapla sınar, çünkü diğer bütün betikler
tohum hesaplarıyla giriş yapıyor. Sıfırlama jetonunu HTTP yanıtından değil
`backend/src/T3.Api/storage/outbox/` altındaki e-posta dosyasından okur: jetonu
yanıta koymak, sıfırlama isteyen herkese hesabı devretmek olurdu.

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
>
> **Sabit sayı ve sabit ad kullanmayın.** Betikler birbirinin verisini yiyor:
> `e2e_faz3` ve `e2e_faz4` her koşuda bir girişimi pasife alıyor, `e2e_faz3` bir
> kullanıcı bırakıyor. Kayda **adıyla** bakan üç kontrol bu yüzden ikinci koşuda
> çöküyordu ve düşen kontrol ürünü değil veri durumunu ölçüyordu; hepsi artık
> kapsamdan/veriden türetiliyor. Aynı sınıftan bir tuzak daha: silme zincirinin
> dokümanı kapattığı kontrolü, seçilen girişimin tohumda dosyası **olduğu**
> varsayımına dayanıyordu. Betik artık silinecek kaydı başarı sayısına bakarak
> seçiyor ve silmeden önce kendi dosyasını yüklüyor — kontrol tohum düzenini
> değil zinciri ölçüyor. Buna rağmen tam yeşil bir zincir hâlâ temiz
> tohum verisi ister — yukarıdaki tur sırası bunun için var.
>
> **Sayfa iskeleti ile veri aynı an gelmiyor.** `wait_for` başlığı gördüğü an
> dönüyor; satırlar sorgudan sonra basılıyor. "Yeni program düğmesi var" ve
> "kimliği doğrulanmamış olay ayırt ediliyor" kontrolleri bu yüzden ekranda
> "… yükleniyor" varken ölçüp düştü — yine ürünü değil zamanlamayı ölçen bir
> kontrol. `render_faz7.py` içindeki `wait_loaded()` yükleniyor metni kaybolana
> kadar bekliyor. Aynı dosyada KVKK onay kapısı kontrolü, onay tarayıcıda
> hatırlandığı için ölçmeden önce `localStorage`'daki kaydı siliyor: yoksa
> ikinci koşuda kutu işaretli açılır ve "varsayılan kapalı" kontrolü profilin
> durumunu ölçerdi.
>
> Docker köprüsü host tarafında `DOWN` düşerse (`ip -br addr show | grep br-`)
> yayımlanan Postgres portu bağlantıyı kabul edip iletmez ve API açılışta
> "Timeout during reading attempt" verir. Teşhisi buradan başlatın; çözüm root
> ister (`sudo ip link set <br> up` ya da docker'ı yeniden başlatmak).
