using T3.Domain.Startups;

namespace T3.Application.Features.Startups.SearchStartups;

/// <summary>
/// Liste satırı. Hassas alanlar (vergi no, iletişim, ekip kişisel verisi)
/// bilinçli olarak hiç taşınmaz — maskelemek yerine hiç sormamak, KVKK
/// açısından en güvenli yol.
///
/// <see cref="AmountsVisible"/> ayrı bir alan olarak taşınıyor çünkü
/// <see cref="TotalInvestment"/>'ın <c>null</c> olması iki farklı anlama
/// gelebilir: yatırım kaydı yok, ya da tutarı görme yetkisi yok. Arayüz
/// "—" ile kilit simgesi arasında seçim yapabilmek için bu ayrımı bilmeli.
/// </summary>
public sealed record StartupListItemResponse(
    Guid Id,
    string Name,
    Sector Sector,
    string? City,
    StartupStatus Status,
    string? LogoUrl,
    DateOnly? FoundedOn,
    IReadOnlyList<string> TechnologyAreas,
    string? LatestProgramName,
    int ProgramCount,
    int AchievementCount,
    decimal? TotalInvestment,
    bool AmountsVisible,
    string Currency);
