using T3.Domain.Approvals;

namespace T3.Application.Features.Approvals;

/// <summary>
/// Onay ve ret aynı sonucu üretir: isteğin yeni durumu, kim ne zaman karar
/// verdi, gerekçe. İki dilim için tek şekil kullanılıyor — aynı özelliğin iki
/// ucu oldukları için (bkz. <c>SessionUserResponse</c> emsali).
///
/// <paramref name="AppliedEntityId"/> yalnızca onayda dolu olur: arayüz karardan
/// sonra kullanıcıyı doğrudan değişen kayda götürebilsin.
/// </summary>
public sealed record ReviewChangeRequestResponse(
    Guid Id,
    ChangeRequestStatus Status,
    DateTimeOffset ReviewedAt,
    string? ReviewNote,
    string? AppliedEntityType = null,
    Guid? AppliedEntityId = null);
