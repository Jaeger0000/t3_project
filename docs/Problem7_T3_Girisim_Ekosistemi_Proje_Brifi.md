# Proje Brifi — T3 Girişim Ekosistemi Yönetim Sistemi (Problem 7)

*Şartname ve Problem Kitapçığı'ndan derlenen, geliştirme için gerekli tüm bilgiler.*

---

## 1. Problem Tanımı

**Koordinatörlük:** Girişim Merkezi Koordinatörlüğü

**Tek cümle özet:** Programdan yatırıma, T3 girişimcilik ekosisteminin tek kurumsal hafızası ve karar destek platformu.

### Neden bu proje gerekli?

Farklı T3 girişimcilik programlarından (Take Off, Ön Kuluçka, TEKNOFEST vb.) geçen girişimlerin profil, program, satış, yatırım, ekip, başarı ve doküman verileri tek bir güncel sistemde yönetilemiyor. Bunun sonucunda:

- Girişim geçmişi parçalı kalıyor (her program kendi tablosunu/dosyasını tutuyor)
- Güncel veriye ulaşmak zaman alıyor
- Yatırım, program ve etkinlik kararları zorlaşıyor (veri dağınık olduğu için karar destek verisi eksik)

### Amaç

Tüm T3 girişimcilik faaliyetlerinden geçen girişimleri **tek profil altında** izleyen, girişimlerin kendi verilerini güncelleyebildiği ve yöneticilerin karar desteği alabildiği **merkezi platform** oluşturmak.

### Beklenen sonuç

Bir girişimin T3 ile ilk temasından güncel satış ve yatırım durumuna kadar tüm yolculuğu **tek kartta** görünür; yöneticiler ekosistemi filtreleyip raporlayabilir.

---

## 2. Kullanıcı Rolleri ve Yetkileri

| Rol | Yetki / Sorumluluk |
|---|---|
| **Super Admin** | Tüm girişimleri, programları, kullanıcıları ve onay süreçlerini merkezi olarak yönetir; tüm raporlara erişir. |
| **Program Yöneticisi** | Yalnızca sorumlu olduğu programdaki girişimleri ekler, günceller; not ve belge süreçlerini yönetir. |
| **Startup Kullanıcısı** | Kendi girişim profilini, satış/yatırım bilgilerini, başarılarını ve dokümanlarını günceller (admin onayına tabi). |
| **Karar Verici** | Doğrulanmış girişim verileri ve dashboard göstergeleri üzerinden program, etkinlik ve yatırım kararlarını destekler (salt okunur + rapor/filtre). |

> Tasarım ilkesi: aynı girişim verisi, farklı yetki ve karar ihtiyaçlarıyla tek doğrulanmış kaynaktan (single source of truth) sunulmalı.

---

## 3. MVP Kapsamı — Zorunlu Altı Gereksinim

> **Kritik kural (Şartname + Kitapçık ortak vurgusu):** Bir madde eksikse takım bir sonraki değerlendirme aşamasına geçemez. Aşağıdaki dört ana blok ve alt kalemler MVP için **zorunludur**, opsiyonel değildir.

1. **Merkezi girişim profili ve girişim kartı**
   Genel bilgiler, teknoloji, ekip, ürün, program geçmişi ve temel göstergeler **tek girişim kartında** tutulur.

2. **Program geçmişi ve gelişim yolculuğu**
   Girişimin katıldığı T3 programları, dönemleri, durumları ve önemli gelişim adımları **kronolojik** olarak izlenir.

3. **Startup portalı + admin onayı**
   Girişim kendi satış, yatırım, ekip ve başarı verilerini günceller; değişiklikler **admin onayıyla** yayınlanır (doğrudan yayın yok — onay akışı zorunlu).

4. **Satış, yatırım, başarı ve doküman takibi**
   Ciro, ihracat, yatırım turları, hibeler, ödüller ve temel dokümanlar **yapılandırılmış** biçimde saklanır (serbest metin değil, alan bazlı veri modeli).

### Önerilen veri modeli (üstteki gereksinimlerden türetilen)

- **Girişim (Startup):** id, ad, kuruluş tarihi, sektör, teknoloji alanı, ekip bilgisi, ürün açıklaması, iletişim bilgisi, durum (aktif/pasif)
- **Program Katılımı (ProgramParticipation):** girişim_id, program_adı (Take Off / Ön Kuluçka / TEKNOFEST / diğer), dönem, başlangıç-bitiş tarihi, durum, önemli notlar
- **Finansal/Başarı Kaydı (Achievement):** girişim_id, tip (ciro, ihracat, yatırım turu, hibe, ödül), tutar/detay, tarih, kaynak/doğrulama
- **Doküman (Document):** girişim_id, tip, dosya, yüklenme tarihi, onay durumu
- **Onay Kaydı (ApprovalLog):** kim, ne değişti, ne zaman, onaylayan admin, durum (bekliyor/onaylandı/reddedildi)

---

## 4. Kullanıcı Akışları

> Kitapçıkta yalnızca üç akış başlığı (Süper Admin, Program Yöneticisi, Startup Kullanıcısı) yer alıyor; detay adımlar kaynak slaytlarda görüntülenemedi. Aşağıdaki adımlar MVP gereksinimlerinden ve diğer problemlerdeki akış kalıbından türetilmiş öneridir — geliştirme sırasında netleştirilmeli.

- **Akış 01 — Super Admin:** Sistemi kurar → programları/kategorileri tanımlar → kullanıcı rollerini atar → bekleyen onayları işler → ekosistem genelinde rapor alır.
- **Akış 02 — Program Yöneticisi:** Kendi programına girişim ekler/ilişkilendirir → girişim bilgilerini günceller → not/belge ekler → program bazlı durumu izler.
- **Akış 03 — Startup Kullanıcısı:** Girişim profilini oluşturur/düzenler → satış-yatırım-başarı verisi girer → doküman yükler → admin onayı sonrası veri yayınlanır.
- **Akış 04 — Karar Verici (implicit):** Dashboard'a girer → ekosistemi filtreler (program, sektör, dönem) → girişim kartlarını inceler → rapor çıkarır.

---

## 5. Genel Program Bağlamı (Şartname'den — geliştirmeyi etkileyen kurallar)

### 5.1. Teslim Edilecek Çıktılar (Görevlerin Teslimi — 26 Ağustos, saat 10.00)

1. İş Modeli Canvası
2. Geliştirilen ön prototipi anlatan video
3. Girişim sunumu

### 5.2. Creathon Süreci (5-6 Eylül)

- **1. Gün:** Açılış, problem analizi, yoğun mentörlük, proje üretimi, sunum eğitimi.
- **2. Gün:** Proje üretimi, final testleri, Demo Day jüri sunumu (5 dk pitching + 5 dk soru-cevap) ve ödül töreni.
- Her takım Creathon süresince **yalnızca bu problem (Problem 7)** üzerinde çalışır — kapsam değişikliği yok.

### 5.3. Veri Güvenliği ve KVKK (zorunlu, tüm çözümler için geçerli)

- Sistem, **6698 sayılı KVKK**'ya uygun tasarlanmalı.
- Hassas verilerin (girişim finansal bilgisi, kişisel iletişim bilgisi vb.) **anonimleştirilmesi/maskelenmesi takımın sorumluluğunda**.
- Bu proje özelinde: girişim finansal verileri (ciro, yatırım tutarı) ve ekip üyesi kişisel bilgileri hassas veri kategorisinde değerlendirilmeli; erişim rol bazlı sınırlandırılmalı.

### 5.4. Eğitim Modülleri (referans alınabilecek teknik/metodolojik kaynaklar)

- **Yapay Zekâ Eğitimi:** LLM mimarisi, Prompt Engineering, RAG ile kurumsal veri entegrasyonu, Claude/Lovable ile hızlı prototipleme — Problem 7'nin "karar destek" ve dashboard özelliklerinde AI destekli özetleme/filtreleme için kullanılabilir.
- **Siber Güvenlik Eğitimi:** Veri güvenliği, KVKK uyumu, hassas veri anonimleştirme, web/mobil/API güvenliği.
- **Fikir Geliştirme ve Sunum Eğitimi:** Kök neden analizi, MVP'ye dönüştürme, 5 dakikalık pitching teknikleri (Demo Day için).
- **Marka Oluşturma Eğitimi:** Ürünün konumlandırılması ve tanıtımı.

### 5.5. Ödüller ve Kaynaklar

- Finale kalan takımlara **Claude ve Lovable** platformlarında ücretsiz API / geliştirici kredi desteği sağlanıyor — prototip bu araçlarla hızlı geliştirilebilir.
- Başarılı bulunan takımlar T3 Girişim Merkezi Ön Kuluçka Programı'na doğrudan kabul, TEKNOFEST Şanlıurfa'da sergileme imkânı, staj imkânı ve özel yemek daveti kazanıyor.
- İhtiyaç duyan ekiplere T3 Vakfı Ünalan Merkez Binası ve İTÜ Özdemir Bayraktar Tasarım ve Prototip Merkezi'nde çalışma alanı sağlanıyor.

### 5.6. Fikri Mülkiyet ve Gizlilik

- Program süresince erişilen T3 Vakfı operasyonel verileri, kılavuzları ve kurumsal bilgileri **yalnızca program kapsamında** kullanılabilir; üçüncü taraflarla paylaşılamaz.
- Geliştirilen çözümün fikri mülkiyeti başvuru sırasında kabul edilen program koşulları çerçevesinde düzenlenir; dereceye giren ve vakıf bünyesine entegre edilmesi uygun görülen projeler için ayrı iş birliği/kullanım şartları belirlenir.
- İçeriğin özgün olması esastır — kopyalama tespit edilirse takım programdan çıkarılır.

### 5.7. Genel Kurallar (özet)

- Takım 3-4 kişi; bireysel başvuru yok.
- Teslim tarih/saatlerine tam uyum zorunlu — geç teslim değerlendirmeye alınmaz.
- Eğitim, mentörlük ve Creathon oturumlarına tam katılım zorunlu.

---

## 6. Geliştirme İçin Öncelik Sırası (öneri)

1. Veri modeli + rol bazlı kimlik doğrulama (MVP madde 1 + roller)
2. Merkezi girişim kartı CRUD (MVP madde 1)
3. Program geçmişi / kronolojik zaman çizelgesi (MVP madde 2)
4. Startup portalı (kendi veri girişi) + admin onay akışı (MVP madde 3)
5. Satış/yatırım/başarı/doküman yapılandırılmış kayıt modülü (MVP madde 4)
6. Karar Verici dashboard'u (filtre, rapor, opsiyonel AI destekli özet)
7. Demo Day için: İş Modeli Canvas, tanıtım videosu, sunum hazırlığı

---

*Kaynak belgeler: [T3_Vakfi_Bursiyer_Yapay_Zeka_Creathon_Sartnamesi.md](T3_Vakfi_Bursiyer_Yapay_Zeka_Creathon_Sartnamesi.md), [T3_Vakfi_Bursiyer_Yapay_Zeka_Creathon_Problemler_Kitapcigi.md](T3_Vakfi_Bursiyer_Yapay_Zeka_Creathon_Problemler_Kitapcigi.md)*
