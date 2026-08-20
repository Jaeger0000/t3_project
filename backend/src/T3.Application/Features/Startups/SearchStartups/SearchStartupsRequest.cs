using T3.Application.Common.Paging;
using T3.Domain.Startups;

namespace T3.Application.Features.Startups.SearchStartups;

public sealed record SearchStartupsRequest : PagedRequest
{
    /// <summary>Ad, ürün açıklaması ve şehirde geçen serbest metin araması.</summary>
    public string? Q { get; init; }

    public Sector? Sector { get; init; }
    public StartupStatus? Status { get; init; }

    /// <summary>Girişimin en az bir dönemine katıldığı program.</summary>
    public Guid? ProgramId { get; init; }

    public string? City { get; init; }
    public StartupSort Sort { get; init; } = StartupSort.Name;
}

public enum StartupSort
{
    Name = 0,
    Newest = 1,
    MostInvestment = 2
}
