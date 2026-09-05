using T3.Domain.Startups;

namespace T3.Application.Features.Reports.EcosystemStats;

/// <summary>
/// Pano süzgeçleri. Girişim aramasıyla aynı alanlar kullanılıyor ki panodaki
/// bir dilime tıklayıp listeye geçmek aynı sorguyu tekrar etsin.
/// </summary>
public sealed record EcosystemStatsRequest(
    Guid? ProgramId = null,
    Sector? Sector = null,
    string? City = null,
    int? Year = null);
