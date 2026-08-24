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
