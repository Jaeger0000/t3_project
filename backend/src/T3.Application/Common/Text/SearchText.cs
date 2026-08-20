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
}
