using T3.Application.Common.Paging;
using T3.Domain.Registrations;
using T3.Domain.Startups;

namespace T3.Application.Features.Registrations.ListRegistrationRequests;

public sealed record ListRegistrationRequestsRequest : PagedRequest
{
    public RegistrationRequestStatus? Status { get; init; }
}

/// <summary>
/// Liste yanıtı. <c>PasswordHash</c> kasıtlı olarak burada yok — başvuru
/// listesi SuperAdmin'in gözünden bile şifre özetini görmemeli, onay akışı
/// hash'i asla dışarı sızdırmadan doğrudan kullanıcı satırına taşır.
/// </summary>
public sealed record RegistrationRequestResponse(
    Guid Id,
    string Email,
    string FullName,
    string StartupName,
    Sector Sector,
    string? City,
    string? ContactPhone,
    RegistrationRequestStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason);
