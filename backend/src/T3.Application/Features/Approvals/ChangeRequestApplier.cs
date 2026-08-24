using FluentValidation;
using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Application.Features.Achievements;
using T3.Application.Features.Documents;
using T3.Application.Features.Startups;
using T3.Application.Features.Startups.Team;
using T3.Domain.Achievements;
using T3.Domain.Approvals;
using T3.Domain.Documents;
using T3.Domain.Startups;

namespace T3.Application.Features.Approvals;

/// <summary>
/// Onaylanan önerinin denetim izine yazılacak sonucu. Uygulayıcı kaydı
/// <em>yazmaz</em>: değişikliği izlenen varlığa işler, <c>SaveChanges</c>
/// çağrısını onay handler'ına bırakır. Böylece "istek onaylandı" ile "veri
/// değişti" tek işlemde birlikte kalıcı olur — biri yazılıp diğeri
/// yazılmadığında kuyrukta onaylanmış ama uygulanmamış öneri kalırdı.
/// </summary>
public sealed record AppliedChange(
    string EntityType, Guid EntityId, object? Before, object? After);

/// <summary>
/// Onaylanan öneriyi hedef varlığa uygular — akışın "yayına girme" adımı.
///
/// Gönderim ile onay arasında günler geçebilir, bu yüzden burada gövde yeniden
/// doğrulanır ve hedefin hâlâ var olduğu doğrulanır. Gönderim anındaki
/// doğrulamaya güvenmek yetmez: girişim adı bu arada başkası tarafından
/// alınmış, ekip üyesi silinmiş olabilir.
/// </summary>
public sealed class ChangeRequestApplier(
    IAppDbContext db,
    ICurrentUser currentUser,
    IDocumentStorage storage,
    IValidator<StartupWriteModel> startupValidator,
    IValidator<TeamMemberWriteModel> teamValidator,
    IValidator<AchievementWriteModel> achievementValidator)
{
    public Task<Result<AppliedChange>> ApplyAsync(ChangeRequest request, CancellationToken ct) =>
        request.TargetType switch
        {
            ChangeTargetType.Startup => ApplyStartupAsync(request, ct),
            ChangeTargetType.TeamMember => ApplyTeamMemberAsync(request, ct),
            ChangeTargetType.Achievement => ApplyAchievementAsync(request, ct),
            ChangeTargetType.Document => ApplyDocumentAsync(request, ct),
            _ => Failure("Bu hedef türü için onay akışı henüz açık değil.")
        };

    /// <summary>
    /// Reddedilen önerinin arkasında bıraktığı kaynakları serbest bırakır.
    ///
    /// Şu an tek durum doküman yüklemesi: dosya öneri gönderilirken depoya
    /// yazılıyor, reddedilirse orada kalmamalı. Uygulamanın karşılığı olarak
    /// aynı bileşende duruyor — "onaylanınca ne olur / reddedilince ne olur"
    /// sorusunun iki cevabı yan yana okunabilsin.
    /// </summary>
    public async Task DiscardAsync(ChangeRequest request, CancellationToken ct)
    {
        if (request.TargetType != ChangeTargetType.Document
            || request.Operation != ChangeOperation.Create)
            return;

        if (ChangeRequestJson.TryDeserialize<DocumentProposalModel>(request.PayloadJson)
            is not { } proposal)
            return;

        try
        {
            await storage.DeleteAsync(proposal.StoragePath, ct);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Dosya silinemezse ret işlemi yine de geçerli: kayıt oluşmadığı
            // için sızıntı yok, artık dosya temizliği ayrı bir bakım işi.
        }
    }

    private async Task<Result<AppliedChange>> ApplyStartupAsync(
        ChangeRequest request, CancellationToken ct)
    {
        if (request.Operation != ChangeOperation.Update)
            return Error.Conflict("Girişim profili için yalnızca güncelleme önerisi uygulanabilir.");

        if (ChangeRequestJson.TryDeserialize<StartupWriteModel>(request.PayloadJson) is not { } model)
            return Error.Conflict("Öneri gövdesi okunamadı; bu öneri uygulanamaz.");

        if (await ValidateAsync(startupValidator, model, ct) is { } invalid)
            return invalid;

        var startup = await db.Startups.FirstOrDefaultAsync(s => s.Id == request.StartupId, ct);

        if (startup is null)
            return Error.Conflict("Girişim artık mevcut değil; öneri uygulanamaz.");

        // Ad tekilliği gönderimden sonra bozulmuş olabilir: aynı adı bir başkası
        // bu arada almış olabilir. Onay anında yeniden bakılmadan uygulanırsa
        // veritabanı kısıtı hatayla patlar, inceleyici nedenini göremez.
        var normalized = SearchText.Normalize(model.Name);

        var nameTaken = await db.Startups
            .AnyAsync(s => s.Id != startup.Id && s.Name.ToLower() == normalized, ct);

        if (nameTaken)
            return Error.Conflict(
                $"\"{model.Name.Trim()}\" adlı başka bir girişim kayıtlı. Öneri uygulanamaz.");

        var before = StartupAuditSnapshot.Of(startup);
        model.ApplyTo(startup);

        return new AppliedChange(
            nameof(Startup), startup.Id, before, StartupAuditSnapshot.Of(startup));
    }

    private async Task<Result<AppliedChange>> ApplyTeamMemberAsync(
        ChangeRequest request, CancellationToken ct)
    {
        if (request.Operation == ChangeOperation.Create)
        {
            if (ChangeRequestJson.TryDeserialize<TeamMemberWriteModel>(request.PayloadJson)
                is not { } newModel)
                return Error.Conflict("Öneri gövdesi okunamadı; bu öneri uygulanamaz.");

            if (await ValidateAsync(teamValidator, newModel, ct) is { } newInvalid)
                return newInvalid;

            var created = new TeamMember { StartupId = request.StartupId };
            newModel.ApplyTo(created);
            db.TeamMembers.Add(created);

            return new AppliedChange(
                nameof(TeamMember), created.Id, null, created.ToResponse());
        }

        if (request.TargetId is not { } memberId)
            return Error.Conflict("Önerinin hedef kaydı belirtilmemiş; uygulanamaz.");

        // StartupId koşulu korunuyor: öneri sahibi girişimin dışındaki bir
        // kaydı hedefleyemez, veri bozulmuş olsa bile uygulama yayılmasın.
        var member = await db.TeamMembers
            .FirstOrDefaultAsync(m => m.Id == memberId && m.StartupId == request.StartupId, ct);

        if (member is null)
            return Error.Conflict("Ekip üyesi artık mevcut değil; öneri uygulanamaz.");

        var snapshot = member.ToResponse();

        if (request.Operation == ChangeOperation.Delete)
        {
            member.IsDeleted = true;
            return new AppliedChange(nameof(TeamMember), member.Id, snapshot, null);
        }

        if (ChangeRequestJson.TryDeserialize<TeamMemberWriteModel>(request.PayloadJson)
            is not { } model)
            return Error.Conflict("Öneri gövdesi okunamadı; bu öneri uygulanamaz.");

        if (await ValidateAsync(teamValidator, model, ct) is { } invalidUpdate)
            return invalidUpdate;

        model.ApplyTo(member);

        return new AppliedChange(
            nameof(TeamMember), member.Id, snapshot, member.ToResponse());
    }

    /// <summary>
    /// Başarı/finans kaydı önerisini uygular (MVP #3 + #4 kesişimi).
    ///
    /// Onaylanan kayıt <em>doğrulanmış</em> olarak işaretlenir ve doğrulayan
    /// olarak onaylayan yetkili yazılır: "bu ciro rakamını kim doğruladı"
    /// sorusunun cevabı kayıtta durmalı.
    /// </summary>
    private async Task<Result<AppliedChange>> ApplyAchievementAsync(
        ChangeRequest request, CancellationToken ct)
    {
        if (request.Operation == ChangeOperation.Create)
        {
            if (ChangeRequestJson.TryDeserialize<AchievementWriteModel>(request.PayloadJson)
                is not { } newModel)
                return Error.Conflict("Öneri gövdesi okunamadı; bu öneri uygulanamaz.");

            if (await ValidateAsync(achievementValidator, newModel, ct) is { } newInvalid)
                return newInvalid;

            var created = AchievementKinds.NewFor(newModel.Kind, request.StartupId);
            newModel.ApplyTo(created);
            MarkVerified(created);

            db.Achievements.Add(created);

            return new AppliedChange(
                nameof(Achievement), created.Id, null,
                created.ToResponse(StartupVisibility.All));
        }

        if (request.TargetId is not { } achievementId)
            return Error.Conflict("Önerinin hedef kaydı belirtilmemiş; uygulanamaz.");

        var achievement = await db.Achievements
            .FirstOrDefaultAsync(a => a.Id == achievementId && a.StartupId == request.StartupId, ct);

        if (achievement is null)
            return Error.Conflict("Başarı kaydı artık mevcut değil; öneri uygulanamaz.");

        var snapshot = achievement.ToResponse(StartupVisibility.All);

        if (request.Operation == ChangeOperation.Delete)
        {
            achievement.IsDeleted = true;
            achievement.DeletedAt = DateTimeOffset.UtcNow;

            return new AppliedChange(nameof(Achievement), achievement.Id, snapshot, null);
        }

        if (ChangeRequestJson.TryDeserialize<AchievementWriteModel>(request.PayloadJson)
            is not { } model)
            return Error.Conflict("Öneri gövdesi okunamadı; bu öneri uygulanamaz.");

        if (await ValidateAsync(achievementValidator, model, ct) is { } invalid)
            return invalid;

        // Tür, kaydın kimliğinin parçası (TPH ayrıştırıcısı) ve yerinde
        // değiştirilemez. Gönderimde de engelleniyor; kayıt bu arada başka bir
        // türle değiştirilmiş olabileceği için onayda yeniden bakılıyor.
        if (model.Kind != AchievementKinds.Of(achievement))
            return Error.Conflict("Kayıt türü değiştirilemez; bu öneri uygulanamaz.");

        model.ApplyTo(achievement);
        MarkVerified(achievement);

        return new AppliedChange(
            nameof(Achievement), achievement.Id, snapshot,
            achievement.ToResponse(StartupVisibility.All));
    }

    /// <summary>
    /// Doküman önerisini uygular. Yükleme önerisinde dosya zaten depoda;
    /// burada yalnızca kayıt satırı oluşuyor — dosyanın "yayına girmesi"
    /// listede görünür hâle gelmesi demek.
    /// </summary>
    private async Task<Result<AppliedChange>> ApplyDocumentAsync(
        ChangeRequest request, CancellationToken ct)
    {
        if (request.Operation == ChangeOperation.Create)
        {
            if (ChangeRequestJson.TryDeserialize<DocumentProposalModel>(request.PayloadJson)
                is not { } proposal)
                return Error.Conflict("Öneri gövdesi okunamadı; bu öneri uygulanamaz.");

            var document = new Document
            {
                StartupId = request.StartupId,
                Type = proposal.Type,
                FileName = proposal.FileName,
                StoragePath = proposal.StoragePath,
                ContentType = proposal.ContentType,
                SizeBytes = proposal.SizeBytes,

                // Yükleyen, öneriyi gönderen girişim kullanıcısı; onaylayan
                // değil. Denetim izinde dosyayı kimin getirdiği kaybolmamalı.
                UploadedByUserId = request.SubmittedByUserId,
                UploadedAt = request.SubmittedAt
            };

            db.Documents.Add(document);

            return new AppliedChange(
                nameof(Document), document.Id, null, document.ToResponse(null));
        }

        if (request.TargetId is not { } documentId)
            return Error.Conflict("Önerinin hedef kaydı belirtilmemiş; uygulanamaz.");

        var existing = await db.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.StartupId == request.StartupId, ct);

        if (existing is null)
            return Error.Conflict("Doküman artık mevcut değil; öneri uygulanamaz.");

        if (request.Operation != ChangeOperation.Delete)
            return Error.Conflict("Doküman için yalnızca kaldırma önerisi uygulanabilir.");

        var before = existing.ToResponse(null);

        // Dosya depoda kalıyor (bkz. DeleteDocumentHandler): kayıt pasife
        // alınıyor, içerik geri alınabilir durumda duruyor.
        existing.IsDeleted = true;
        existing.DeletedAt = DateTimeOffset.UtcNow;

        return new AppliedChange(nameof(Document), existing.Id, before, null);
    }

    private void MarkVerified(Achievement achievement)
    {
        achievement.IsVerified = true;
        achievement.VerifiedByUserId = currentUser.UserId;
        achievement.VerifiedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gövdeyi gönderimdeki kurallarla yeniden doğrular. Onay akışı doğrudan
    /// yazmaya göre gevşek olamaz: aksi hâlde onay, doğrulamayı atlamanın yolu
    /// olurdu. Hata mesajı inceleyiciye gösterilir — kendi gövdesi olmadığı
    /// için nedenini bilmesi gerekir.
    /// </summary>
    private static async Task<Error?> ValidateAsync<T>(
        IValidator<T> validator, T model, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(model, ct);

        if (result.IsValid)
            return null;

        var reasons = string.Join(" ", result.Errors.Select(e => e.ErrorMessage).Distinct());
        return Error.Conflict($"Öneri artık geçerli değil: {reasons}");
    }

    private static Task<Result<AppliedChange>> Failure(string message) =>
        Task.FromResult<Result<AppliedChange>>(Error.Conflict(message));
}
