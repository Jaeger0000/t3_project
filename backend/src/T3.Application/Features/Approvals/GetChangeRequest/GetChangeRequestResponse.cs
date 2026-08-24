using T3.Domain.Approvals;

namespace T3.Application.Features.Approvals.GetChangeRequest;

/// <summary>
/// Onay ekranının tamamı: kim ne öneriyor, hangi alanlar değişiyor, karar
/// verilebilir mi.
/// </summary>
public sealed record ChangeRequestDetailResponse(
    Guid Id,
    Guid StartupId,
    string StartupName,
    ChangeTargetType TargetType,
    ChangeOperation Operation,
    Guid? TargetId,
    string TargetLabel,
    string OperationLabel,
    string SubmittedByName,
    DateTimeOffset SubmittedAt,
    ChangeRequestStatus Status,
    string? ReviewedByName,
    DateTimeOffset? ReviewedAt,
    string? ReviewNote,

    /// <summary>Arayüz onayla/reddet düğmelerini buna göre gösterir; gerçek yetki kontrolü handler'da.</summary>
    bool CanReview,

    /// <summary>
    /// Öneri gövdesi okunabildi mi. Okunamayan bir öneri onaylanamaz; ekran
    /// "boş diff" göstermek yerine durumu açıkça söyler.
    /// </summary>
    bool IsReadable,

    int ChangedFieldCount,
    IReadOnlyList<DiffFieldResponse> Fields);
