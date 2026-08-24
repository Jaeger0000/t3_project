using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Features.Startups;
using T3.Domain.Documents;

namespace T3.Application.Features.Documents.DeleteDocument;

/// <summary>
/// Dokümanı pasife alır. Fiziksel dosya bilinçli olarak duruyor: kayıt yanlışlıkla
/// silinirse geri alınabilsin ve denetim izindeki "indirildi" satırlarının
/// işaret ettiği içerik ortadan kalkmasın. Kalıcı silme ayrı bir saklama
/// politikası işi, tek tıklık bir işlem değil.
/// </summary>
public sealed class DeleteDocumentHandler(
    IAppDbContext db,
    StartupEditGuard guard,
    IAuditWriter audit)
{
    public async Task<Result<bool>> Handle(Guid startupId, Guid documentId, CancellationToken ct)
    {
        var access = await guard.ResolveEditableAsync(startupId, ct);
        if (!access.IsSuccess)
            return access.Error!;

        var document = await db.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.StartupId == startupId, ct);

        if (document is null)
            return Error.NotFound("Doküman bulunamadı.");

        var before = document.ToResponse(null);

        document.IsDeleted = true;
        document.DeletedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            "Document.Delete", nameof(Document), document.Id,
            before: before, ct: ct);

        return true;
    }
}
