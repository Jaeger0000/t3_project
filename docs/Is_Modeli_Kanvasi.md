# İş Modeli Kanvası — T3 Girişim Ekosistemi Yönetim Sistemi

> **Taslak — ekip onayı bekliyor.** 26 Ağustos teslimindeki üç çıktıdan biri
> (kanvas + prototip videosu + sunum). Buradaki her kutu ürünün **çalışan**
> hâlinden türetildi; henüz yapılmamış bir şey "var" gibi yazılmadı — yapılmamış
> olanlar en sonda ayrı başlıkta duruyor.
>
> Bağlam: bu bir ticari girişim değil, **T3 Vakfı'nın kendi operasyonel
> ihtiyacına** yazılmış bir iç platform. Kanvas bu yüzden "kaç para kazanır"
> yerine "hangi maliyeti düşürür, hangi kararı iyileştirir" sorusuna cevap
> veriyor. Vakıf dışına açılma senaryosu ayrı bir kutuda, açıkça varsayım
> olarak işaretli.

---

## 1. Müşteri segmentleri (kullanıcı rolleri)

| Segment | Kim | Bugünkü acısı |
|---|---|---|
| **Süper Yönetici** | T3 Girişim Merkezi operasyon ekibi | Ekosistemin tamamını gören tek kişi olmak zorunda; veri kimde, hangi Excel'de bilmiyor |
| **Program Yöneticisi** | Ön Kuluçka / Kuluçka / TEKNOFEST / DENEYAP program sorumluları | Kendi programındaki girişimi başka programın verisiyle birleştiremiyor; kendi tablosunu kendisi tutuyor |
| **Karar Verici** | Vakıf yönetimi, mütevelli, sponsor/paydaş | "Ekosistem ne durumda" sorusuna cevap için rapor sipariş etmek zorunda; cevap haftalar sonra ve elle derlenmiş geliyor |
| **Girişim (Startup) kullanıcısı** | Girişimin kurucu/temsilcisi | Aynı bilgiyi her programa, her formda yeniden yazıyor; kimin ne gördüğünü bilmiyor |

**Segmentlerin ortak noktası:** hepsi *aynı* girişim verisine bakıyor ama farklı
yetki ve farklı karar ihtiyacıyla. Ürünün tek cümlelik varlık sebebi bu.

## 2. Değer önerisi

> **Dağınık girişim verisini tek doğrulanmış kayda indiren, kimin ne gördüğünü
> koda gömülü kurallarla belirleyen kurumsal hafıza.**

Somut karşılıkları:

- **Tek girişim kartı:** profil, ekip, ürün, program geçmişi, finansal/başarı
  kayıtları ve dokümanlar tek ekranda (MVP #1).
- **Gelişim yolculuğu:** program katılımları ve dönüm noktaları kronolojik —
  "bu girişim bizimle nereden nereye geldi" sorusunun tek ekranlık cevabı (MVP #2).
- **Onay akışı:** girişim kendi verisini günceller ama hiçbir tabloya doğrudan
  yazmaz; her değişiklik önce öneri, sonra kayıt (MVP #3). Kurumsal hafızanın
  güvenilirliği buradan geliyor.
- **Yapılandırılmış finans/başarı verisi:** ciro, ihracat, yatırım turu, hibe,
  ödül — serbest metin değil alan bazlı (MVP #4). Toplanabilir, filtrelenebilir,
  raporlanabilir olmasının sebebi bu.
- **KVKK'yı ürünün içine gömmek:** finansal tutar ve kişisel iletişim bilgisi
  role göre maskeleniyor; maskeleme kuralı tek yerde ve **okunabilir** duruyor,
  "kim neyi neden göremiyor" sorusu koddan gösterilebiliyor.
- **Yapay zekâ, veriyi dışarı taşımadan:** sistem MCP sunucusu olarak
  yayımlanıyor; harici ajan (Claude) kendi jetonuyla bağlanıyor ve **kullanıcının
  yetkisi kadar** görüyor. Yani AI, yetki sisteminin dışına çıkan bir yan kapı
  değil.

## 3. Kanallar

- Vakıf iç ağı / kurum içi dağıtım (tek origin web uygulaması, mobil tarayıcıda
  çalışıyor).
- Program başvuru ve kabul süreçlerine gömülü kullanım: girişim programa
  girerken hesabı açılıyor, portal ilk temas noktası oluyor.
- Karar vericiye giden düzenli rapor: panodan CSV dışa aktarma (maskeleme
  kuralları CSV'de de geçerli).
- MCP üzerinden AI istemcileri (Claude Desktop/Code) — analist masasındaki
  kanal.

## 4. Müşteri ilişkileri

- **Girişim tarafı:** self-servis portal + onay geri bildirimi (öneri kuyruğa
  girdi / yayına alındı / reddedildi ve gerekçesi).
- **Program yöneticisi tarafı:** kendi programının kuyruğu ve kapsamı; başka
  programın verisi ekranına hiç düşmüyor.
- **Karar verici tarafı:** self-servis pano, rapor talebi gerektirmiyor.
- **Kurumsal hafıza:** denetim izi. Kim ne zaman ne değiştirdi, kim hangi
  dokümanı indirdi, kim başarısız giriş denedi — hesap verebilirliğin kaydı.

## 5. Gelir akışları

Bu bir iç platform: doğrudan gelir yok, **ölçülebilir tasarruf ve kazanılmış
karar kalitesi** var.

- **Elle rapor derleme maliyeti ortadan kalkıyor.** Bugün program bazlı Excel'leri
  birleştirmek her rapor turunda birkaç kişi-günü. Pano bunu anlık hâle getiriyor.
- **Mükerrer veri girişi ortadan kalkıyor:** girişim aynı bilgiyi her programa
  yeniden yazmıyor.
- **Kurumsal hafıza kaybı önleniyor:** program yöneticisi değiştiğinde veri
  kişiyle gitmiyor. (Bu kalemin parasal karşılığı yok ama en pahalı olanı.)
- **Sponsor/paydaş raporlaması hızlanıyor:** ekosistem karnesi hazır.

> **Varsayım (test edilmedi):** aynı ürün başka kuluçka/hızlandırma
> programlarına (üniversite TTO'ları, teknoparklar, kurumsal girişimcilik
> programları) kurulum + yıllık lisans modeliyle sunulabilir. Rol modeli,
> maskeleme ve onay akışı bu kurumlarda da aynı; program tanımları veri.
> Bu senaryo kanvasa **hipotez** olarak yazıldı, plan olarak değil.

## 6. Kilit kaynaklar

- **Veri modeli ve yetki mimarisi.** Satır düzeyi kapsam (`IStartupScope`), alan
  düzeyi maskeleme (`StartupVisibility`), onay kuyruğu kapsamı
  (`IChangeRequestScope`), program sahipliği (`ProgramAccessGuard`) — ürünün
  kopyalanması zor olan kısmı burası, ekran değil.
- **T3'ün gerçek program yapısı bilgisi** (Ön Kuluçka → Kuluçka → Hızlandırma →
  TEKNOFEST/DENEYAP hattı).
- Çalışan altyapı: .NET 8 API + PostgreSQL + React arayüz, tek konteynerde
  dağıtılabilir.
- Denetim izi ve KVKK metinleri (aydınlatma + kullanım şartları; hukuki içerik
  onay bekliyor).

## 7. Kilit faaliyetler

- Program/dönem tanımlarının güncel tutulması (arayüzden yönetiliyor).
- Onay kuyruğunun işletilmesi — ürünün kalite kapısı.
- Veri doğruluğu: girişimden gelen finansal kayıtların kaynak/doğrulama alanıyla
  birlikte tutulması.
- KVKK bakımı: maskeleme kurallarının rol değişikliklerinde gözden geçirilmesi,
  aydınlatma metninin güncel kalması.
- Güvenlik bakımı: oturum, hız sınırı, denetim izi gözden geçirmeleri.

## 8. Kilit ortaklıklar

- **T3 Vakfı program ekipleri** — verinin hem kaynağı hem tüketicisi.
- **TEKNOFEST ve DENEYAP** organizasyonları — program geçmişinin bir kısmı
  oradan geliyor.
- **Anthropic / Claude** — MCP istemcisi ve API kredisi (Creathon kapsamında).
- **Barındırma:** vakıf içi sunucu ya da bulut; ürün tek origin çalıştığı için
  ikisi de bir ters vekilin arkasında aynı şekilde kuruluyor.
- Hukuk/uyum danışmanlığı — KVKK metinlerinin onayı.

## 9. Maliyet yapısı

- **Geliştirme:** Creathon takımı (4 kişi) + Creathon sonrası bakım.
- **Barındırma:** tek uygulama konteyneri + PostgreSQL. Ölçek küçük (yüzlerce
  girişim, onlarca kullanıcı) — maliyet kalemin en küçüğü.
- **AI:** MCP modeli gereği **uygulamanın içinde model maliyeti yok**; harici
  ajan kendi jetonuyla bağlanıyor. Panel içi sorular yerel planlayıcıyla
  yanıtlanıyor (ücretsiz). Anahtar girilirse maliyet kullanıcı başına ölçülebilir
  hâle geliyor.
- **Uyum:** KVKK metinlerinin hukuki gözden geçirmesi (tek seferlik + yıllık).

---

## Rakip/alternatif karşılaştırması (dürüst hâli)

| Alternatif | Neden yetmiyor |
|---|---|
| **Excel + paylaşılan klasör** | Bugünkü durum. Rol bazlı erişim yok, onay akışı yok, iz yok, aynı veri birden çok yerde |
| **Genel amaçlı CRM** (HubSpot vb.) | Girişim kartı, program dönemi, gelişim yolculuğu kavramları yok; KVKK maskelemesi alan bazında kurulamıyor; onay akışı "deal stage"e zorlanıyor |
| **Airtable/Notion tabloları** | Hızlı başlıyor, rol bazlı alan maskeleme ve denetim izi tarafında duvara çarpıyor; veri kurum dışında |
| **Sıfırdan her programa ayrı form** | Bugünkü dağınıklığın kaynağı |

## Bu kanvasta olmayan (bilinçli)

- **Fiyatlandırma tablosu yok** — iç platform; dış satış senaryosu hipotez
  olarak işaretli.
- **Kullanıcı sayısı/ciro projeksiyonu yok** — uydurma sayı, jüri sorusunda
  savunulamayan tek şeydir.
- **Hukuki içerik onayı** alınmadı; KVKK metinleri ürün içinde "Taslak" işaretli.

## Kanıt: bu kanvas neye dayanıyor

Kanvastaki her değer iddiasının çalışan karşılığı var; ayrıntı ve doğrulama
kayıtları için: [README — Durum](../README.md#durum),
[Denetim düzeltme planı](Denetim_Duzeltme_Plani.md). Ürün 191 birim testi,
310 uçtan uca kontrol ve headless tarayıcıda 346 render kontrolüyle
doğrulanıyor; bu sayılar kanvasın "çalışıyor" iddiasının dayanağı.
