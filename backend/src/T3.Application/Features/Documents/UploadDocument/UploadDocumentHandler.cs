using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Application.Features.Approvals;
using T3.Domain.Approvals;
using T3.Domain.Documents;

namespace T3.Application.Features.Documents.UploadDocument;

public sealed record DocumentUploadRequest(
    Guid StartupId,
    DocumentType Type,
    string? FileName,
    long SizeBytes,
    Stream Content);

/// <summary>
/// Yüklemenin sonucu. <paramref name="Applied"/> yükleme ile önerinin aynı
/// uçtan geçtiğini arayüze anlatır: girişim kullanıcısı dosyayı yükler ama
/// listede göremez, çünkü kaydı henüz oluşmamıştır.
/// </summary>
public sealed record DocumentUploadResponse(
    bool Applied,
    DocumentResponse? Document,
    Guid? ChangeRequestId,
    string Message);

/// <summary>
/// Doküman yükleme (MVP #4) — tek uç, iki yol.
///
/// Yetkili doğrudan yazar; girişim kullanıcısı onay isteği üretir. İkisi ayrı
/// uç noktalar olsaydı "girişim hiçbir tabloya doğrudan yazmaz" kuralı iki
/// yerde ayrı ayrı korunmak zorunda kalırdı; kararı tek handler'da tutmak
/// kuralın tek okunabilir yerini sağlıyor.
/// </summary>
public sealed class UploadDocumentHandler(
    IAppDbContext db,
    IStartupScope scope,
    IChangeRequestScope approvals,
    ICurrentUser currentUser,
    IDocumentStorage storage,
    IAuditWriter audit)
{
    public async Task<Result<DocumentUploadResponse>> Handle(
        DocumentUploadRequest request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Type))
            return Error.Validation("Geçersiz doküman türü.");

        // Kapsam dışındaki girişim "yok" sayılır.
        var startup = await scope.Apply(db.Startups.AsNoTracking())
            .FirstOrDefaultAsync(s => s.Id == request.StartupId, ct);

        if (startup is null)
            return Error.NotFound("Girişim bulunamadı.");

        var direct = scope.CanEditDirectly(startup.Id);
        var proposal = approvals.CanSubmit && currentUser.StartupId == startup.Id;

        if (!direct && !proposal)
            return Error.Forbidden("Bu girişime doküman yükleme yetkiniz yok.");

        var check = DocumentUploadRules.Check(request.FileName, request.SizeBytes);
        if (!check.IsSuccess)
            return check.Error!;

        var fileName = DocumentUploadRules.SafeFileName(request.FileName)!;
        var contentType = check.Value!;
        var extension = Path.GetExtension(fileName);

        if (!await DocumentUploadRules.ContentMatchesExtensionAsync(request.Content, extension, ct))
            return Error.Validation("Dosya içeriği uzantıyla uyuşmuyor.");

        // Dosya her iki yolda da önce depoya yazılıyor: yükleme tek seferlik,
        // içeriği onaya kadar bekletecek bir yer yok. Reddedilen öneride
        // ChangeRequestApplier.DiscardAsync dosyayı siler.
        var storagePath = await storage.SaveAsync(request.Content, fileName, contentType, ct);

        return direct
            ? await SaveDirectAsync(request, fileName, contentType, storagePath, ct)
            : await SubmitProposalAsync(request, fileName, contentType, storagePath, ct);
    }

    private async Task<Result<DocumentUploadResponse>> SaveDirectAsync(
        DocumentUploadRequest request, string fileName, string contentType,
        string storagePath, CancellationToken ct)
    {
        var uploadedAt = DateTimeOffset.UtcNow;

        var document = new Document
        {
            StartupId = request.StartupId,
            Type = request.Type,
            FileName = fileName,
            StoragePath = storagePath,
            ContentType = contentType,
            SizeBytes = request.SizeBytes,
            UploadedByUserId = currentUser.UserId ?? Guid.Empty,
            UploadedAt = uploadedAt
        };

        db.Documents.Add(document);
        await db.SaveChangesAsync(ct);

        var response = document.ToResponse(null);

        await audit.WriteAsync(
            "Document.Create", nameof(Document), document.Id,
            after: response, ct: ct);

        return new DocumentUploadResponse(
            true, response, null, $"\"{fileName}\" yüklendi.");
    }

    private async Task<Result<DocumentUploadResponse>> SubmitProposalAsync(
        DocumentUploadRequest request, string fileName, string contentType,
        string storagePath, CancellationToken ct)
    {
        var proposal = new DocumentProposalModel(
            request.Type, fileName, storagePath, contentType, request.SizeBytes);

        var changeRequest = new ChangeRequest
        {
            StartupId = request.StartupId,
            SubmittedByUserId = currentUser.UserId ?? Guid.Empty,
            SubmittedAt = DateTimeOffset.UtcNow,
            TargetType = ChangeTargetType.Document,
            Operation = ChangeOperation.Create,
            PayloadJson = ChangeRequestJson.Serialize(proposal),
            Status = ChangeRequestStatus.Pending
        };

        db.ChangeRequests.Add(changeRequest);
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            "ChangeRequest.Submit", nameof(ChangeRequest), changeRequest.Id,
            after: new
            {
                changeRequest.StartupId,
                changeRequest.TargetType,
                changeRequest.Operation,
                proposal.FileName,
                proposal.SizeBytes
            },
            ct: ct);

        return new DocumentUploadResponse(
            false, null, changeRequest.Id,
            $"\"{fileName}\" onaya gönderildi; yetkili onayladıktan sonra listede görünecek.");
    }
}
