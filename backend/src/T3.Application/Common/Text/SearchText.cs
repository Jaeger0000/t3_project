namespace T3.Application.Common.Text;

public static class SearchText
{
    /// <summary>
    /// Arama terimini veritabanının <c>lower()</c> davranışıyla aynı hizaya getirir.
    ///
    /// Kritik ayrıntı: <c>ToLowerInvariant()</c> noktalı büyük İ'yi (U+0130)
    /// <em>olduğu gibi bırakır</em> — invariant küçültmede bu harfin karşılığı
    /// yoktur. PostgreSQL'in <c>lower()</c> işlevi ise onu düz <c>i</c> yapar.
    /// Sonuç: kolon "istanbul"a inerken parametre "İstanbul" kalır ve
    /// "İstanbul" araması hiçbir zaman eşleşmez. Sessiz, fark edilmesi zor bir
    /// hata; bu yüzden İ önce ASCII I'ya indirilip sonra küçültülür.
    ///
    /// Türkçe verinin geri kalanında iki tarafın küçültmesi örtüşür: noktasız ı
    /// zaten küçüktür, ASCII I her iki tarafta da i olur, ç/ş/ö/ü/ğ değişmez.
    /// Davranış <c>SearchTextTests</c> ile kilitlenmiştir.
    /// </summary>
    public static string Normalize(string value) =>
        value.Trim().Replace('İ', 'I').ToLowerInvariant();

    /// <summary>
    /// Türkçe aksanları ASCII karşılığına katlar: "saglik" araması "Sağlık"
    /// kaydını bulsun.
    ///
    /// <see cref="Normalize"/>'dan <b>ayrı</b> tutuluyor ve bu ayrım bilinçli:
    /// Normalize'ın çıktısı veritabanındaki <em>katlanmamış</em> kolonla
    /// karşılaştırılıyor (e-posta eşitliği, isim tekilliği). Katlamayı oraya
    /// eklemek terimi "oguz" yaparken kolonu "oğuz" bırakır ve giriş sessizce
    /// çalışmaz hâle gelirdi.
    ///
    /// Katlama yalnızca <b>iki tarafın da katlandığı</b> yerlerde kullanılır:
    /// bellekte bu metot, sorguda aynı harf çiftlerini SQL <c>replace()</c>
    /// zincirine çeviren <c>StartupSearch</c>. Gösterim etiketleri hiç
    /// katlanmaz — kullanıcı "Sağlık" görmeye devam eder.
    /// </summary>
    public static string Fold(string value)
    {
        var lowered = Normalize(value);

        // Sıra önemsiz ama liste kısa ve okunur olsun diye alfabetik: her çift
        // "kullanıcının klavyesinde olmayabilir" varsayımıyla tek yönlü.
        return lowered
            .Replace("ç", "c")
            .Replace("ğ", "g")
            .Replace("ı", "i")
            .Replace("ö", "o")
            .Replace("ş", "s")
            .Replace("ü", "u");
    }
}
