using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Paging;
using T3.Application.Common.Results;
using T3.Domain.Identity;

namespace T3.Application.Features.Audit.ListAuditLogs;

/// <summary>
/// Denetim izi sorgusu — yalnızca SuperAdmin.
///
/// İz, maskelenmiş alanların ham hâlini içerir (değişikliği kanıtlayabilmesi
/// için içermek zorunda). Bu yüzden erişim rol kapısına ek olarak burada da
/// kontrol edilir: uç noktanın politikası unutulsa veya handler MCP üzerinden
/// çağrılsa bile kapı kapalı kalır.
/// </summary>
public sealed class ListAuditLogsHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<PagedResult<AuditLogListItemResponse>>> Handle(
        ListAuditLogsRequest request, CancellationToken ct)
    {
        if (currentUser.Role != UserRole.SuperAdmin)
            return Error.Forbidden("Denetim izine yalnızca sistem yöneticisi erişebilir.");

        var query = db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            var entityType = request.EntityType.Trim();
            query = query.Where(a => a.EntityType == entityType);
        }

        if (request.EntityId is { } entityId)
            query = query.Where(a => a.EntityId == entityId);

        if (request.ActorUserId is { } actorUserId)
            query = query.Where(a => a.ActorUserId == actorUserId);

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            // Ön ek eşleşmesi: "Startup" araması Startup.Create ve
            // Startup.Update satırlarını birlikte getirir. Eylem adları kod
            // tarafından üretildiği için harf durumu sabittir, normalleştirme
            // gerekmiyor.
            var action = request.Action.Trim();
            query = query.Where(a => a.Action.StartsWith(action));
        }

        if (request.From is { } from)
            query = query.Where(a => a.OccurredAt >= from);

        if (request.To is { } to)
            query = query.Where(a => a.OccurredAt <= to);

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new
            {
                a.Id,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.ActorUserId,
                a.ActorRole,
                a.IpAddress,
                a.UserAgent,
                a.OccurredAt,
                a.BeforeJson,
                a.AfterJson
            })
            .ToListAsync(ct);

        // Aktör adı ayrı sorguyla çözülüyor: AuditLog'un kullanıcıya yabancı
        // anahtarı yok ve olmaması doğru — kullanıcı kaydı silinse bile iz
        // ayakta kalmalı. Silinmiş kullanıcılar da dahil ediliyor.
        var actorIds = rows
            .Where(r => r.ActorUserId is not null)
            .Select(r => r.ActorUserId!.Value)
            .Distinct()
            .ToList();

        var actorNames = await db.Users
            .IgnoreQueryFilters()
            .Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var items = rows.Select(r => new AuditLogListItemResponse(
            r.Id,
            r.Action,
            r.EntityType,
            r.EntityId,
            r.ActorUserId,
            ActorName(r.ActorUserId, actorNames),
            r.ActorRole,
            r.IpAddress,
            r.UserAgent,
            r.OccurredAt,
            r.BeforeJson,
            r.AfterJson)).ToList();

        return new PagedResult<AuditLogListItemResponse>(
            items, request.Page, request.PageSize, total);
    }

    /// <summary>
    /// Aktör etiketi. Üç ayrı durum var ve üçü de kullanıcıya farklı bir şey
    /// anlatıyor: kimlik hiç doğrulanmadı (başarısız giriş), kullanıcı kaydı
    /// sonradan silindi, ya da ad biliniyor.
    /// </summary>
    private static string ActorName(Guid? actorUserId, IReadOnlyDictionary<Guid, string> names) =>
        actorUserId is not { } id
            ? "(kimlik doğrulanmadı)"
            : names.GetValueOrDefault(id) ?? $"(kayıt yok: {id})";
}
