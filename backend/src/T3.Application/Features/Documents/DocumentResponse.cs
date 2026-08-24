using System.Globalization;
using T3.Domain.Documents;

namespace T3.Application.Features.Documents;

/// <summary>
/// Doküman üstverisi. Dosyanın kendisi hiçbir zaman bu gövdede taşınmaz;
/// indirme ayrı bir uçtan, yetki yeniden kontrol edilerek yapılır.
/// </summary>
public sealed record DocumentResponse(
    Guid Id,
    Guid StartupId,
    DocumentType Type,
    string TypeLabel,
    string FileName,
    string ContentType,
    long SizeBytes,
    string SizeLabel,
    Guid UploadedByUserId,
    string? UploadedByName,
    DateTimeOffset UploadedAt);

public static class DocumentLabels
{
    public static string Type(DocumentType type) => type switch
    {
        DocumentType.PitchDeck => "Sunum",
        DocumentType.Financials => "Finansal tablo",
        DocumentType.Incorporation => "Kuruluş belgesi",
        DocumentType.Patent => "Patent / fikrî mülkiyet",
        DocumentType.Report => "Rapor",
        DocumentType.Contract => "Sözleşme",
        _ => "Diğer"
    };

    /// <summary>
    /// İnsan tarafından okunur boyut. Sunucuda üretiliyor çünkü onay
    /// ekranındaki diff satırı da aynı metni gösteriyor — iki yerde
    /// biçimlendirmek "1,2 MB" ile "1.2 MB" farkını doğururdu. Kültür açıkça
    /// tr-TR: sunucunun yerel ayarına bırakılırsa aynı dosya iki makinede iki
    /// farklı metin üretir.
    /// </summary>
    public static string Size(long bytes) => bytes switch
    {
        < 1024 => string.Format(Turkish, "{0} B", bytes),
        < 1024 * 1024 => string.Format(Turkish, "{0:0.#} KB", bytes / 1024d),
        _ => string.Format(Turkish, "{0:0.#} MB", bytes / (1024d * 1024d))
    };

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
}

internal static class DocumentResponses
{
    /// <summary>
    /// Varlıktan gövdeye eşleme. Yükleyenin adı çağıranda çözülür: liste
    /// sorgusu tek projeksiyonda getiriyor, tekil yazma yolları ise adı zaten
    /// bilmiyor ve bunun için ayrı sorgu açmaya değmiyor.
    /// </summary>
    public static DocumentResponse ToResponse(this Document d, string? uploadedByName) => new(
        d.Id, d.StartupId, d.Type, DocumentLabels.Type(d.Type),
        d.FileName, d.ContentType, d.SizeBytes, DocumentLabels.Size(d.SizeBytes),
        d.UploadedByUserId, uploadedByName, d.UploadedAt);
}
