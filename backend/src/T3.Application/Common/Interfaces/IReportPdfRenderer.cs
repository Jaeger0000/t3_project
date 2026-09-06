namespace T3.Application.Common.Interfaces;

/// <summary>Modelin ürettiği tek bir bölüm: başlık + gövde metni.</summary>
public sealed record ReportSectionContent(string Title, string Body);

/// <summary>
/// PDF'e dökülecek rapor. Şablon (marka, sayfa düzeni) sağlayıcıda sabit
/// kodludur — burada yalnızca doldurulacak veri durur, model çıktısı dahil
/// hiçbir HTML/düzen bilgisi taşımaz.
/// </summary>
public sealed record StartupReportDocument(
    string StartupName,
    string? SectorLabel,
    string? City,
    string? StatusLabel,
    DateTimeOffset GeneratedAt,
    string GeneratedByName,
    string GeneratedByRoleLabel,
    IReadOnlyList<ReportSectionContent> Sections);

/// <summary>
/// Rapor PDF'ini üretir. Application yalnızca bu arayüzü bilir; hangi PDF
/// kütüphanesinin kullanıldığı Infrastructure'da kalır (sağlayıcıya özel API
/// Application katmanına sızmaz).
/// </summary>
public interface IReportPdfRenderer
{
    byte[] Render(StartupReportDocument document);
}
