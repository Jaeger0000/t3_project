using T3.Domain.Startups;

namespace T3.Application.Features.Reports.ExportStartups;

/// <summary>
/// Dışa aktarma süzgeçleri. Girişim aramasıyla aynı alanları taşır ama sayfalama
/// yok: kullanıcı ekranda ne süzdüyse onun tamamını indirir.
/// </summary>
public sealed record ExportStartupsRequest(
    string? Q = null,
    Sector? Sector = null,
    StartupStatus? Status = null,
    Guid? ProgramId = null,
    string? City = null);
