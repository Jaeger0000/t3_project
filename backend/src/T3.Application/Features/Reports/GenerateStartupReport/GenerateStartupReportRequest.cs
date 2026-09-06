namespace T3.Application.Features.Reports.GenerateStartupReport;

/// <summary>
/// Rapor talebi. Sorgu dizesinden geliyor (GET — veri değiştirmiyor, CSV
/// aktarımıyla aynı desen). İkisi de boşsa (<see cref="Sections"/> boş/null,
/// <see cref="CustomFocus"/> boş) tüm standart bölümler üretilir — "tam
/// rapor" varsayılan davranıştır. Bilinmeyen bölüm anahtarı sessizce
/// yok sayılır (bkz. <see cref="ReportSections.Find"/> kullanan
/// GenerateStartupReportHandler.ResolveSections); tek gerçek doğrulama
/// (özel istek uzunluğu) handler'da yapılıyor.
/// </summary>
public sealed record GenerateStartupReportRequest(
    IReadOnlyList<string>? Sections,
    string? CustomFocus);
