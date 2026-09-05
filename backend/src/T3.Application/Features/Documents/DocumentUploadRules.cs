using T3.Application.Common.Results;

namespace T3.Application.Features.Documents;

/// <summary>
/// Yükleme kuralları. Doğrulama endpoint filtresinde değil burada duruyor:
/// filtre JSON gövdesi üzerinde çalışıyor, dosya yüklemesi ise multipart —
/// aynı kurallar iki yolda da geçerli olsun diye handler'ın çağırdığı tek
/// noktaya alındı.
/// </summary>
public static class DocumentUploadRules
{
    public const long MaxSizeBytes = 20 * 1024 * 1024;

    /// <summary>
    /// İzin verilen uzantılar ve <em>bizim</em> atadığımız içerik tipi.
    ///
    /// İstemcinin gönderdiği Content-Type başlığına güvenilmiyor: saldırgan
    /// ".pdf" adlı dosyayı "text/html" olarak işaretleyip indirme anında
    /// tarayıcıda çalıştırmayı deneyebilir. Tip uzantıdan türetilip kaydediliyor.
    /// </summary>
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".csv"] = "text/csv",
        [".txt"] = "text/plain",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg"
    };

    public static string AllowedExtensionList => string.Join(", ", Allowed.Keys);

    /// <summary>
    /// Uzantı-içerik doğrulaması için sihirli bayt imzaları. Yalnızca güvenilir
    /// bir imzası olan türler burada — <c>.csv</c>/<c>.txt</c> serbest metin
    /// olduğu için her içerik "geçerli" sayılır, imza kontrolü atlanır.
    /// ".pdf" uzantılı bir dosyanın gerçekte HTML/betik olması bu yüzden bugüne
    /// kadar yalnızca uzantıya bakılarak kabul ediliyordu (bkz. G-08,
    /// Guvenlik_Denetimi_ve_Iyilestirme_Plani.md).
    /// </summary>
    private static readonly Dictionary<string, byte[][]> MagicBytes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = [[0x25, 0x50, 0x44, 0x46]], // %PDF
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        // Office Open XML (docx/xlsx/pptx) bir ZIP arşividir.
        [".docx"] = [[0x50, 0x4B, 0x03, 0x04]],
        [".xlsx"] = [[0x50, 0x4B, 0x03, 0x04]],
        [".pptx"] = [[0x50, 0x4B, 0x03, 0x04]],
        // Eski ikili Office biçimi (doc/xls/ppt) OLE Compound File'dır.
        [".doc"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
        [".xls"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
        [".ppt"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
    };

    /// <summary>
    /// Akışın ilk baytlarını uzantının beklediği imzayla karşılaştırır.
    /// Kontrolden sonra akış başa sarılır — çağıran taraf içeriği ayrıca
    /// depoya yazacak. İmzası tanımlı olmayan uzantılar (csv, txt) her zaman
    /// geçer.
    /// </summary>
    public static async Task<bool> ContentMatchesExtensionAsync(
        Stream content, string extension, CancellationToken ct)
    {
        if (!MagicBytes.TryGetValue(extension, out var signatures))
            return true;

        var maxLength = signatures.Max(s => s.Length);
        var buffer = new byte[maxLength];
        var read = await content.ReadAsync(buffer.AsMemory(0, maxLength), ct);

        if (content.CanSeek)
            content.Seek(0, SeekOrigin.Begin);

        return signatures.Any(signature =>
            read >= signature.Length && buffer.AsSpan(0, signature.Length).SequenceEqual(signature));
    }

    /// <summary>
    /// Dosya adını ve boyutu doğrular; geçerliyse saklanacak içerik tipini döner.
    /// </summary>
    public static Result<string> Check(string? fileName, long sizeBytes)
    {
        var clean = SafeFileName(fileName);

        if (clean is null)
            return Error.Validation("Dosya adı zorunludur.");

        if (clean.Length > 200)
            return Error.Validation("Dosya adı en fazla 200 karakter olabilir.");

        if (sizeBytes <= 0)
            return Error.Validation("Boş dosya yüklenemez.");

        if (sizeBytes > MaxSizeBytes)
            return Error.Validation(
                $"Dosya en fazla {MaxSizeBytes / (1024 * 1024)} MB olabilir.");

        var extension = Path.GetExtension(clean);

        if (string.IsNullOrEmpty(extension) || !Allowed.TryGetValue(extension, out var contentType))
            return Error.Validation($"Bu dosya türü kabul edilmiyor. İzin verilenler: {AllowedExtensionList}");

        return contentType;
    }

    /// <summary>
    /// Yol bileşenlerinden arındırılmış dosya adı. Tarayıcılar bazı durumlarda
    /// tam yol gönderir; adı olduğu gibi saklamak indirmede yol karışıklığına
    /// yol açar. Depodaki fiziksel ad zaten rastgele üretiliyor, bu ad yalnızca
    /// gösterim ve indirme başlığı için.
    /// </summary>
    public static string? SafeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var name = Path.GetFileName(fileName.Trim().Replace('\\', '/'));

        // Satır sonu ve tırnak Content-Disposition başlığına enjeksiyon yolu;
        // ad gösterimlik olduğu için temizlemek bilgi kaybettirmiyor.
        name = new string(name.Where(c => !char.IsControl(c) && c is not '"').ToArray()).Trim();

        return string.IsNullOrWhiteSpace(name) ? null : name;
    }
}
