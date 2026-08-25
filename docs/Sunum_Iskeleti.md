# Girişim sunumu — slayt iskeleti

> **Taslak — ekip onayı bekliyor.** 26 Ağustos teslimindeki üçüncü çıktı.
> 12 slayt, 5 dakikalık anlatıya göre ölçüldü. Her slaytta **tek mesaj** var;
> slayt metni ekranda görüneni tekrar etmiyor, konuşmacı notu ne söyleneceğini
> yazıyor. Demo akışı ve jüri soruları: [Demo_Senaryosu.md](Demo_Senaryosu.md).

| # | Slayt | Tek mesaj | Ekranda ne var | Konuşmacı notu |
|---|---|---|---|---|
| 1 | **Kapak** | — | Ürün adı, takım, Problem 7 | "T3 Girişim Ekosistemi Yönetim Sistemi — Problem 7." |
| 2 | **Problem** | Aynı girişimin üç farklı hikâyesi var | Üç kaynak görseli: program Excel'i, başvuru formu, kurucunun sunumu — ortada aynı girişim adı | "Veri yok değil; **dağınık ve çelişkili**. Hangisi doğru sorusunun cevabı kişiye bağlı." |
| 3 | **Kime ne kaybettiriyor** | Dört rol, dört ayrı acı | 4 satır: Süper Yönetici / Program Yöneticisi / Karar Verici / Girişim + her birinin tek cümlelik acısı | "Program sorumlusu değişince kurumsal hafıza kişiyle gidiyor — en pahalı kayıp bu." |
| 4 | **Çözüm — tek cümle** | Tek doğrulanmış kayıt + rol bazlı erişim | Büyük punto tek cümle, altında dört rol ikonu | Değer önerisi cümlesi (kanvastan). |
| 5 | **Girişim kartı** (MVP #1) | Her şey tek ekranda | Kart ekran görüntüsü, sekmeler işaretli | "Profil, ekip, ürün, program geçmişi, finans, doküman — altı sekme, tek kayıt." |
| 6 | **Gelişim yolculuğu** (MVP #2) | Nereden nereye | Kronoloji ekran görüntüsü | "Ön Kuluçka'dan TEKNOFEST'e; hangi dönem, hangi dönüm noktası." |
| 7 | **Onay akışı** (MVP #3) | Girişim hiçbir tabloya doğrudan yazmaz | Portal → öneri → diff → onay akış şeması (4 kutu) | "Kurumsal hafızanın güvenilirliği buradan geliyor: her değişiklik önce öneri." |
| 8 | **Yapılandırılmış veri** (MVP #4) | Serbest metin değil, alan bazlı | Başarı türleri tablosu (ciro/ihracat/yatırım/hibe/ödül) + CSV çıktısı | "Toplanabilir, filtrelenebilir, raporlanabilir olmasının sebebi bu." |
| 9 | **KVKK** | Aynı kart, farklı rol | Yan yana iki ekran görüntüsü: aynı kart, biri tutarlı biri kilitli | "Maskeleme kuralı kodda **tek yerde** ve okunabilir; 'kim neyi neden göremiyor' sorusunu koddan gösterebiliyoruz. Yazma yolunda da korunuyor: göremediğiniz alanı kaydederken silmiyorsunuz." |
| 10 | **AI — MCP kararı** | AI yetki sisteminin yanından dolaşmıyor | Şema: Claude → `POST /mcp` → aynı handler'lar → aynı kapsam/maskeleme | "Modeli uygulamaya gömmedik. Analist kendi jetonuyla bağlanıyor ve **kendi yetkisi kadar** görüyor." |
| 11 | **Kurumsal olgunluk** | Çalışıyor ve doğrulanıyor | Rakamlar: 191 birim testi · 310 uçtan uca kontrol · 346 render kontrolü · varsayılan kapalı yetki · denetim izi · tek origin dağıtım | "Doğrulama zincirimiz üç katmanlı; ekranın gerçekten açıldığını headless tarayıcıda ölçüyoruz." |
| 12 | **Kapanış + yol haritası** | Bugün kurulabilir | Üç madde: bugün hazır / onay bekleyen (KVKK metni) / sıradaki (hata izleme, SMTP, gerçek dağıtım) | "Altı zorunlu MVP maddesi çalışıyor. Kalanı içerik onayı ve ölçek." |

## Tasarım notları

- **Ekran görüntüleri gerçek üründen**, mockup değil. Jüri farkı görüyor.
- Ekran görüntülerinde **rol rozeti kadraja girsin** (sağ üst): slaytın hangi
  gözle bakıldığını tek başına anlatıyor.
- Renk paleti üründen: kurumsal turuncu `#E73A13`, sıcak nötr griler. Slayt ile
  ürün aynı yerden gelmiş görünmeli.
- 9. slayt sunumun **tepe noktası** — en çok provasını yapın.
- Sayı yazan tek slayt 11; oradaki her sayının arkasında koşan bir betik var,
  uydurma sayı koymayın.

## 5 dakikaya sığdırma

Slayt 1–4 → 1:10 · demo (5–9) → 2:20 · 10–11 → 1:00 · 12 → 0:30.
Süre daralırsa **8. slayt** kesilir (MVP #4 zaten 5. slayttaki karttan
görünüyor); 7 ve 9 **kesilmez**.
