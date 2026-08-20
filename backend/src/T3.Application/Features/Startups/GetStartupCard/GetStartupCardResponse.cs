using T3.Domain.Programs;
using T3.Domain.Startups;

namespace T3.Application.Features.Startups.GetStartupCard;

/// <summary>
/// Merkezi girişim kartı (MVP #1). Hassas alanlar yetki yoksa <c>null</c> döner;
/// <see cref="Visibility"/> arayüze "veri yok" ile "yetkiniz yok" ayrımını
/// yapma imkânı verir — kullanıcıya boş alan değil kilit simgesi gösterilir.
/// </summary>
public sealed record StartupCardResponse(
    Guid Id,
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
    StartupStatus Status,
    IReadOnlyList<CardTeamMemberResponse> Team,
    IReadOnlyList<CardParticipationResponse> Programs,
    CardAchievementSummaryResponse Achievements,
    int DocumentCount,
    int MilestoneCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    CardVisibilityResponse Visibility);

public sealed record CardTeamMemberResponse(
    Guid Id,
    string FullName,
    string? Title,
    string? Email,
    string? Phone,
    string? LinkedInUrl,
    bool IsFounder,
    DateOnly? JoinedOn);

public sealed record CardParticipationResponse(
    Guid Id,
    Guid ProgramId,
    string ProgramName,
    ProgramType ProgramType,
    string? Coordinatorship,
    Guid ProgramTermId,
    string TermName,
    ParticipationStatus Status,
    DateOnly JoinedOn,
    DateOnly? LeftOn,
    string? Notes);

public sealed record CardAchievementSummaryResponse(
    int TotalCount,
    int InvestmentRoundCount,
    int AwardCount,
    decimal? TotalInvestment,
    decimal? TotalGrant,
    decimal? LatestAnnualRevenue,
    int? LatestRevenueYear,
    decimal? TotalExport,
    string Currency);

/// <summary>Hangi alan grubunun görülebildiğini arayüze bildirir (KVKK).</summary>
public sealed record CardVisibilityResponse(
    bool ContactDetails,
    bool TaxNumber,
    bool ExactAmounts,
    bool TeamPersonalData,
    bool Documents);
