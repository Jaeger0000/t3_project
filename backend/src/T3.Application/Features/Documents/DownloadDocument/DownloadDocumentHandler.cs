using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Domain.Documents;

namespace T3.Application.Features.Documents.DownloadDocument;

public sealed record DocumentDownloadResponse(
    Stream Content,
    string FileName,
    string ContentType);

/// <summary>
/// Doküman indirme (MVP #4).
///
/// Yetki burada baştan kontrol ediliyor, listede kontrol edilmiş olmasına
/// güvenilmiyor: indirme bağlantısı paylaşılabilir bir URL, listeden geçmeden
/// de çağrılabilir. Her indirme denetim izine yazılıyor — KVKK açısından
/// "bu belgeyi kim indirdi" cevaplanabilir olmak zorunda.
/// </summary>
public sealed class DownloadDocumentHandler(
    IAppDbContext db,
    IStartupScope scope,
    ICurrentUser currentUser,
    IDocumentStorage storage,
    IAuditWriter audit)
{
    public async Task<Result<DocumentDownloadResponse>> Handle(Guid documentId, CancellationToken ct)
    {
        var document = await db.Documents.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (document is null)
            return Error.NotFound("Doküman bulunamadı.");

        // Kapsam dışındaki girişimin dokümanı da "yok": 403 dosyanın
        // varlığını doğrulardı.
        var inScope = await scope.Apply(db.Startups.AsNoTracking())
            .AnyAsync(s => s.Id == document.StartupId, ct);

        if (!inScope)
            return Error.NotFound("Doküman bulunamadı.");

        var visibility = StartupVisibility.For(currentUser, document.StartupId);

        if (!visibility.ShowDocuments)
            return Error.Forbidden("Doküman indirme yetkiniz yok.");

        Stream content;

        try
        {
            content = await storage.OpenReadAsync(document.StoragePath, ct);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            // Kayıt var, dosya yok: sessizce boş dosya döndürmek yerine
            // ayrıştırılabilir bir hata veriyoruz, veri kaybı görünür kalsın.
            return Error.NotFound("Dokümanın dosyası depoda bulunamadı.");
        }
        catch (UnauthorizedAccessException)
        {
            return Error.NotFound("Doküman bulunamadı.");
        }

        await audit.WriteAsync(
            "Document.Download", nameof(Document), document.Id,
            after: new { document.StartupId, document.FileName, document.Type },
            ct: ct);

        return new DocumentDownloadResponse(content, document.FileName, document.ContentType);
    }
}
