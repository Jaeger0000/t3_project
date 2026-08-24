using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;

namespace T3.Application.Features.Documents.ListDocuments;

public sealed record DocumentListResponse(
    Guid StartupId,
    IReadOnlyList<DocumentResponse> Items);

/// <summary>
/// Girişimin dokümanları (MVP #4). Doküman yetkisi olmayan rol listeyi hiç
/// görmez: başarı kayıtlarının aksine burada "satırı göster, içeriği gizle"
/// diye bir orta yol yok — dosya adı tek başına ticari bilgi taşıyabiliyor
/// ("2025_satis_sozlesmesi_ASELSAN.pdf").
/// </summary>
public sealed class ListDocumentsHandler(
    IAppDbContext db,
    IStartupScope scope,
    ICurrentUser currentUser)
{
    public async Task<Result<DocumentListResponse>> Handle(Guid startupId, CancellationToken ct)
    {
        var exists = await scope.Apply(db.Startups.AsNoTracking())
            .AnyAsync(s => s.Id == startupId, ct);

        if (!exists)
            return Error.NotFound("Girişim bulunamadı.");

        var visibility = StartupVisibility.For(currentUser, startupId);

        if (!visibility.ShowDocuments)
            return Error.Forbidden("Doküman görüntüleme yetkiniz yok.");

        var rows = await db.Documents.AsNoTracking()
            .Where(d => d.StartupId == startupId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new
            {
                d.Id,
                d.StartupId,
                d.Type,
                d.FileName,
                d.ContentType,
                d.SizeBytes,
                d.UploadedByUserId,
                d.UploadedAt,
                // Yükleyenin adı ayrı sorgu yerine tek projeksiyonda: liste
                // ekranı "kim yükledi" olmadan denetlenebilir değil.
                UploadedByName = db.Users
                    .Where(u => u.Id == d.UploadedByUserId)
                    .Select(u => u.FullName)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var items = rows
            .Select(d => new DocumentResponse(
                d.Id, d.StartupId, d.Type, DocumentLabels.Type(d.Type),
                d.FileName, d.ContentType, d.SizeBytes, DocumentLabels.Size(d.SizeBytes),
                d.UploadedByUserId, d.UploadedByName, d.UploadedAt))
            .ToList();

        return new DocumentListResponse(startupId, items);
    }
}
