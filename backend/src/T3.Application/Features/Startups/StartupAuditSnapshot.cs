using T3.Domain.Startups;

namespace T3.Application.Features.Startups;

/// <summary>
/// Denetim izine yazılan girişim anlık görüntüsü (KVKK: "kim, neyi, nasıl
/// değiştirdi"). Hassas alanlar dahil edilir — aksi halde iz, değişikliği
/// kanıtlama işlevini kaybeder. Bu veriye erişim <c>/api/audit-logs</c>
/// ucunda yalnızca SuperAdmin ile sınırlıdır.
/// </summary>
public sealed record StartupAuditSnapshot(
    string Name,
    string? LegalName,
    string? TaxNumber,
    DateOnly? FoundedOn,
    Sector Sector,
    IReadOnlyList<string> TechnologyAreas,
    string? ProductDescription,
    string? Website,
    string? LogoUrl,
    string? City,
    string? ContactEmail,
    string? ContactPhone,
    StartupStatus Status)
{
    public static StartupAuditSnapshot Of(Startup s) => new(
        s.Name, s.LegalName, s.TaxNumber, s.FoundedOn, s.Sector,
        [.. s.TechnologyAreas], s.ProductDescription, s.Website, s.LogoUrl,
        s.City, s.ContactEmail, s.ContactPhone, s.Status);
}
