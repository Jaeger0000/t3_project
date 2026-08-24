using System.Text;

namespace T3.Infrastructure.Persistence.Seed;

/// <summary>
/// Demo dokümanlarının içeriğini üretir.
///
/// Gerçek dosya depoya yazılıyor çünkü indirme yolunun çalıştığı ancak
/// indirilen dosya açıldığında görülebiliyor: boş kayıtla liste dolu görünür
/// ama "indir" düğmesi sessizce bozuk kalırdı.
/// </summary>
internal static class SeedDocumentFiles
{
    /// <summary>
    /// Tek sayfalık, geçerli xref tablosuna sahip asgari PDF üretir.
    ///
    /// Metin bilinçli olarak ASCII: PDF'in gömülü olmayan temel yazı tipleri
    /// (Helvetica) Türkçe'ye özgü harfleri (ş, ğ, ı, İ) kodlayamıyor, gömme
    /// yapmak ise demo verisi için gereksiz bir bağımlılık olurdu.
    /// </summary>
    public static byte[] Pdf(string title, params string[] lines)
    {
        var text = new StringBuilder()
            .Append("BT\n/F1 18 Tf\n60 780 Td\n(").Append(Escape(title)).Append(") Tj\n")
            .Append("/F1 11 Tf\n");

        foreach (var line in lines)
            text.Append("0 -24 Td\n(").Append(Escape(line)).Append(") Tj\n");

        var stream = text.Append("ET\n").ToString();

        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] "
                + "/Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream"
        };

        var body = new StringBuilder("%PDF-1.4\n");
        var offsets = new int[objects.Length];

        for (var i = 0; i < objects.Length; i++)
        {
            // Bayt uzunluğu ASCII olduğu için karakter sayısına eşit; xref
            // tablosu bayt konumu istiyor, kısayol burada güvenli.
            offsets[i] = body.Length;
            body.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xrefPosition = body.Length;

        body.Append("xref\n0 ").Append(objects.Length + 1).Append('\n')
            .Append("0000000000 65535 f \n");

        foreach (var offset in offsets)
            body.Append(offset.ToString("D10")).Append(" 00000 n \n");

        body.Append("trailer\n<< /Size ").Append(objects.Length + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xrefPosition).Append("\n%%EOF\n");

        return Encoding.ASCII.GetBytes(body.ToString());
    }

    /// <summary>CSV demo içeriği: finansal tablo dokümanının karşılığı.</summary>
    public static byte[] Csv(params string[] rows) =>
        // UTF-8 BOM: Excel BOM'suz CSV'yi Windows kod sayfasıyla açıp Türkçe
        // harfleri bozuyor.
        Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(string.Join("\r\n", rows) + "\r\n"))
            .ToArray();

    /// <summary>
    /// PDF metin dizgesinde ayraç karakterleri kaçırılır; kaçırılmazsa dosya
    /// bozulur.
    /// </summary>
    private static string Escape(string value) => value
        .Replace("\\", "\\\\")
        .Replace("(", "\\(")
        .Replace(")", "\\)");
}
