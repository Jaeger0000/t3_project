using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Application.Features.Achievements;
using T3.Application.Features.Documents;
using T3.Application.Features.Startups;
using T3.Application.Features.Startups.Team;
using T3.Domain.Approvals;
using T3.Domain.Startups;

namespace T3.Application.Features.Approvals.SubmitChangeRequest;

/// <summary>
/// MVP #3'ün giriş kapısı. Girişim kullanıcısı hiçbir tabloya doğrudan yazmaz;
/// önerdiği değişiklik burada "önce" ve "sonra" gövdesiyle birlikte kuyruğa
/// girer ve ancak yetkili onayladıktan sonra hedef varlığa uygulanır.
/// </summary>
public sealed class SubmitChangeRequestHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IStartupScope startupScope,
    IChangeRequestScope approvals,
    IAuditWriter audit)
{
    public async Task<Result<SubmitChangeRequestResponse>> Handle(
        SubmitChangeRequestRequest request, CancellationToken ct)
    {
        if (!approvals.CanSubmit)
            return Error.Forbidden(
                "Onay isteği yalnızca girişim kullanıcısı tarafından gönderilir. "
                + "Yetkiniz varsa değişikliği doğrudan kaydedebilirsiniz.");

        if (currentUser.StartupId is not { } startupId)
            return Error.Forbidden("Oturumunuz bir girişime bağlı değil.");

        // Kapsam filtresinden geçiyor: rol bağı ile satır bağı ayrı kontrol,
        // ikisini birden atlayan bir yol kalmasın.
        var startup = await startupScope.Apply(db.Startups)
            .FirstOrDefaultAsync(s => s.Id == startupId, ct);

        if (startup is null)
            return Error.NotFound("Girişim bulunamadı.");

        var draft = request.TargetType switch
        {
            ChangeTargetType.Startup => BuildStartupDraft(startup, request),
            ChangeTargetType.TeamMember => await BuildTeamMemberDraftAsync(startupId, request, ct),
            ChangeTargetType.Achievement => await BuildAchievementDraftAsync(startupId, request, ct),
            ChangeTargetType.Document => await BuildDocumentDraftAsync(startupId, request, ct),
            _ => Error.Validation("Bu hedef türü için onay akışı henüz açık değil.")
        };

        if (!draft.IsSuccess)
            return draft.Error!;

        var (beforeJson, payloadJson, fields) = draft.Value!;

        // Değişen alan yoksa öneri kuyruğa girmez: aynı veriyi tekrar
        // gönderen form, inceleyicinin kuyruğunu boş satırlarla doldurur.
        var changedCount = ChangeRequestDiff.ChangedCount(fields);
        if (changedCount == 0)
            return Error.Validation("Öneride değişen bir alan yok.");

        if (await HasPendingDuplicateAsync(startupId, request, ct))
            return Error.Conflict(
                "Bu kayıt için zaten bekleyen bir öneriniz var. "
                + "Yeni öneri göndermek için önce mevcut önerinin sonuçlanmasını bekleyin.");

        var changeRequest = new ChangeRequest
        {
            StartupId = startupId,
            SubmittedByUserId = currentUser.UserId ?? Guid.Empty,
            SubmittedAt = DateTimeOffset.UtcNow,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            Operation = request.Operation,
            PayloadJson = payloadJson,
            BeforeJson = beforeJson,
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
                changeRequest.TargetId,
                ChangedFieldCount = changedCount
            },
            ct: ct);

        return new SubmitChangeRequestResponse(
            changeRequest.Id,
            changeRequest.TargetType,
            changeRequest.Operation,
            changeRequest.Status,
            changeRequest.SubmittedAt,
            changedCount);
    }

    private sealed record Draft(
        string? BeforeJson, string PayloadJson, IReadOnlyList<DiffFieldResponse> Fields);

    private Result<Draft> BuildStartupDraft(Startup startup, SubmitChangeRequestRequest request)
    {
        // Doğrulayıcı bileşimi zorluyor; buradaki kontrol handler'ın MCP
        // üzerinden doğrudan çağrılması hâlinde de kapıyı kapalı tutar.
        if (request.Operation != ChangeOperation.Update || request.Startup is null)
            return Error.Validation("Girişim profili için yalnızca güncelleme önerisi gönderilebilir.");

        var before = StartupWriteModel.From(startup);

        // Kendi verisi: girişim kullanıcısı için maskeleme yok, diff tam görünür.
        var visibility = StartupVisibility.For(currentUser, startup.Id);

        return new Draft(
            ChangeRequestJson.Serialize(before),
            ChangeRequestJson.Serialize(request.Startup),
            ChangeRequestDiff.ForStartup(before, request.Startup, visibility));
    }

    private async Task<Result<Draft>> BuildTeamMemberDraftAsync(
        Guid startupId, SubmitChangeRequestRequest request, CancellationToken ct)
    {
        var visibility = StartupVisibility.For(currentUser, startupId);

        if (request.Operation == ChangeOperation.Create)
        {
            if (request.TeamMember is null)
                return Error.Validation("Ekip üyesi gövdesi zorunludur.");

            return new Draft(
                null,
                ChangeRequestJson.Serialize(request.TeamMember),
                ChangeRequestDiff.ForTeamMember(null, request.TeamMember, visibility));
        }

        if (request.TargetId is not { } memberId)
            return Error.Validation("Güncelleme ve silme önerisinde targetId zorunludur.");

        // StartupId koşulu şart: başka girişimin üyesi için öneri gönderilemez.
        var member = await db.TeamMembers
            .FirstOrDefaultAsync(m => m.Id == memberId && m.StartupId == startupId, ct);

        if (member is null)
            return Error.NotFound("Ekip üyesi bulunamadı.");

        var before = TeamMemberWriteModel.From(member);

        if (request.Operation == ChangeOperation.Delete)
            return new Draft(
                ChangeRequestJson.Serialize(before),
                ChangeRequestJson.EmptyPayload,
                ChangeRequestDiff.ForTeamMember(before, null, visibility));

        if (request.TeamMember is null)
            return Error.Validation("Ekip üyesi gövdesi zorunludur.");

        return new Draft(
            ChangeRequestJson.Serialize(before),
            ChangeRequestJson.Serialize(request.TeamMember),
            ChangeRequestDiff.ForTeamMember(before, request.TeamMember, visibility));
    }

    /// <summary>
    /// Başarı/finans kaydı önerisi (MVP #4). Girişim kendi cirosunu, aldığı
    /// yatırımı ve ödülünü önerir; kayıt ancak yetkili onayladıktan sonra
    /// doğrulanmış sayılır — bu yüzden portaldan gelen veri hiçbir zaman
    /// "doğrulanmış" etiketiyle başlamaz.
    /// </summary>
    private async Task<Result<Draft>> BuildAchievementDraftAsync(
        Guid startupId, SubmitChangeRequestRequest request, CancellationToken ct)
    {
        var visibility = StartupVisibility.For(currentUser, startupId);

        if (request.Operation == ChangeOperation.Create)
        {
            if (request.Achievement is null)
                return Error.Validation("Başarı kaydı gövdesi zorunludur.");

            return new Draft(
                null,
                ChangeRequestJson.Serialize(request.Achievement),
                ChangeRequestDiff.ForAchievement(null, request.Achievement, visibility));
        }

        if (request.TargetId is not { } achievementId)
            return Error.Validation("Güncelleme ve silme önerisinde targetId zorunludur.");

        var achievement = await db.Achievements
            .FirstOrDefaultAsync(a => a.Id == achievementId && a.StartupId == startupId, ct);

        if (achievement is null)
            return Error.NotFound("Başarı kaydı bulunamadı.");

        var before = AchievementWriteModel.From(achievement);

        if (request.Operation == ChangeOperation.Delete)
            return new Draft(
                ChangeRequestJson.Serialize(before),
                ChangeRequestJson.EmptyPayload,
                ChangeRequestDiff.ForAchievement(before, null, visibility));

        if (request.Achievement is null)
            return Error.Validation("Başarı kaydı gövdesi zorunludur.");

        // Tür değişimi doğrudan yazma yolunda da yasak (TPH ayrıştırıcısı
        // yerinde değiştirilemiyor); onay akışının daha gevşek olmaması için
        // aynı kural gönderim anında da uygulanıyor.
        if (request.Achievement.Kind != before.Kind)
            return Error.Validation(
                $"Kayıt türü değiştirilemez ({AchievementLabels.Kind(before.Kind)}). "
                + "Kaydın kaldırılmasını önerip yenisini ekleyin.");

        return new Draft(
            ChangeRequestJson.Serialize(before),
            ChangeRequestJson.Serialize(request.Achievement),
            ChangeRequestDiff.ForAchievement(before, request.Achievement, visibility));
    }

    /// <summary>
    /// Doküman kaldırma önerisi. Yükleme bu uçtan geçmez: dosya multipart
    /// olarak doküman ucuna gider, öneri orada üretilir (bkz.
    /// <c>UploadDocumentHandler</c>).
    /// </summary>
    private async Task<Result<Draft>> BuildDocumentDraftAsync(
        Guid startupId, SubmitChangeRequestRequest request, CancellationToken ct)
    {
        if (request.Operation != ChangeOperation.Delete)
            return Error.Validation(
                "Doküman yüklemesi bu uçtan gönderilmez; dosyayı doküman yükleme ucundan gönderin.");

        if (request.TargetId is not { } documentId)
            return Error.Validation("Doküman kaldırma önerisinde targetId zorunludur.");

        var document = await db.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.StartupId == startupId, ct);

        if (document is null)
            return Error.NotFound("Doküman bulunamadı.");

        var visibility = StartupVisibility.For(currentUser, startupId);

        var before = new DocumentProposalModel(
            document.Type, document.FileName, document.StoragePath,
            document.ContentType, document.SizeBytes);

        return new Draft(
            ChangeRequestJson.Serialize(before),
            ChangeRequestJson.EmptyPayload,
            ChangeRequestDiff.ForDocument(before, null, visibility));
    }

    /// <summary>
    /// Aynı hedef için ikinci bekleyen öneriyi engeller: iki öneri sırayla
    /// onaylandığında ikincisi birincinin sonucunu sessizce geri alır.
    /// Yeni ekip üyesi önerileri hedefsiz olduğu için bu kurala girmez —
    /// bir girişim aynı anda iki farklı kişiyi eklemek isteyebilir.
    /// </summary>
    private async Task<bool> HasPendingDuplicateAsync(
        Guid startupId, SubmitChangeRequestRequest request, CancellationToken ct)
    {
        var pending = db.ChangeRequests.Where(c =>
            c.StartupId == startupId
            && c.Status == ChangeRequestStatus.Pending
            && c.TargetType == request.TargetType);

        if (request.TargetId is { } targetId)
            return await pending.AnyAsync(c => c.TargetId == targetId, ct);

        if (request.TargetType == ChangeTargetType.Startup)
            return await pending.AnyAsync(c => c.TargetId == null, ct);

        return false;
    }
}
