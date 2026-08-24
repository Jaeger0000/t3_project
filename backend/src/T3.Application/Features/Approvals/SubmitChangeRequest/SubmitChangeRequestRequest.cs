using FluentValidation;
using T3.Application.Features.Achievements;
using T3.Application.Features.Startups;
using T3.Application.Features.Startups.Team;
using T3.Domain.Approvals;

namespace T3.Application.Features.Approvals.SubmitChangeRequest;

/// <summary>
/// Girişim kullanıcısının değişiklik önerisi.
///
/// Gövdede <c>startupId</c> yok, bilinçli olarak: hedef girişim oturumdan
/// (<c>ICurrentUser.StartupId</c>) okunur. İstemcinin gönderdiği bir kimlikle
/// çalışsaydı her handler'da "bu kimlik gerçekten senin mi" kontrolü gerekirdi;
/// alanı hiç almamak o hata sınıfını tümden ortadan kaldırıyor.
/// </summary>
public sealed record SubmitChangeRequestRequest(
    ChangeTargetType TargetType,
    ChangeOperation Operation,
    Guid? TargetId,
    StartupWriteModel? Startup,
    TeamMemberWriteModel? TeamMember,
    AchievementWriteModel? Achievement);

/// <summary>
/// Hedef türü, işlem ve gövde üçlüsünün geçerli bileşimlerini zorlar. Bu
/// doğrulayıcı güvenlik sınırının parçası: "Startup + Delete" gibi girişimin
/// kendi kaydını silmesine yol açacak bileşimler burada durdurulur.
/// </summary>
public sealed class SubmitChangeRequestValidator : AbstractValidator<SubmitChangeRequestRequest>
{
    public SubmitChangeRequestValidator()
    {
        RuleFor(x => x.TargetType).IsInEnum().WithMessage("Geçersiz hedef türü.");
        RuleFor(x => x.Operation).IsInEnum().WithMessage("Geçersiz işlem türü.");


        When(x => x.TargetType == ChangeTargetType.Startup, () =>
        {
            RuleFor(x => x.Operation)
                .Equal(ChangeOperation.Update)
                .WithMessage("Girişim kaydı yalnızca güncelleme önerisi olabilir; "
                             + "kayıt oluşturma ve silme onay akışının dışındadır.");

            RuleFor(x => x.Startup)
                .NotNull().WithMessage("Girişim profili önerisinde startup gövdesi zorunludur.");

            RuleFor(x => x.TargetId)
                .Null().WithMessage("Girişim profili önerisinde targetId gönderilmez; "
                                    + "hedef, oturumun bağlı olduğu girişimdir.");
        });

        When(x => x.TargetType == ChangeTargetType.TeamMember, () =>
        {
            RuleFor(x => x.TeamMember)
                .NotNull().WithMessage("Ekip üyesi önerisinde teamMember gövdesi zorunludur.")
                .When(x => x.Operation is ChangeOperation.Create or ChangeOperation.Update);

            RuleFor(x => x.TargetId)
                .NotNull().WithMessage("Güncelleme ve silme önerisinde targetId zorunludur.")
                .When(x => x.Operation is ChangeOperation.Update or ChangeOperation.Delete);

            RuleFor(x => x.TargetId)
                .Null().WithMessage("Yeni ekip üyesi önerisinde targetId gönderilmez.")
                .When(x => x.Operation == ChangeOperation.Create);
        });

        When(x => x.TargetType == ChangeTargetType.Achievement, () =>
        {
            RuleFor(x => x.Achievement)
                .NotNull().WithMessage("Başarı kaydı önerisinde achievement gövdesi zorunludur.")
                .When(x => x.Operation is ChangeOperation.Create or ChangeOperation.Update);

            RuleFor(x => x.TargetId)
                .NotNull().WithMessage("Güncelleme ve silme önerisinde targetId zorunludur.")
                .When(x => x.Operation is ChangeOperation.Update or ChangeOperation.Delete);

            RuleFor(x => x.TargetId)
                .Null().WithMessage("Yeni başarı kaydı önerisinde targetId gönderilmez.")
                .When(x => x.Operation == ChangeOperation.Create);
        });

        // Doküman yüklemesi bu uçtan geçmez: dosya multipart olarak
        // /api/startups/{id}/documents ucuna gider ve öneri orada üretilir.
        // Buradan yalnızca mevcut bir dokümanın kaldırılması önerilebilir.
        When(x => x.TargetType == ChangeTargetType.Document, () =>
        {
            RuleFor(x => x.Operation)
                .Equal(ChangeOperation.Delete)
                .WithMessage("Doküman yüklemesi bu uçtan gönderilmez; "
                             + "dosyayı doküman yükleme ucundan gönderin.");

            RuleFor(x => x.TargetId)
                .NotNull().WithMessage("Doküman kaldırma önerisinde targetId zorunludur.");
        });

        // Gövdeler doğrudan yazma yolundaki doğrulayıcının aynısından geçer:
        // onay akışı, doğrudan yazmaya göre gevşek olamaz.
        RuleFor(x => x.Startup!)
            .SetValidator(new StartupWriteModelValidator())
            .When(x => x.Startup is not null);

        RuleFor(x => x.TeamMember!)
            .SetValidator(new TeamMemberWriteModelValidator())
            .When(x => x.TeamMember is not null);

        RuleFor(x => x.Achievement!)
            .SetValidator(new AchievementWriteModelValidator())
            .When(x => x.Achievement is not null);
    }
}

public sealed record SubmitChangeRequestResponse(
    Guid Id,
    ChangeTargetType TargetType,
    ChangeOperation Operation,
    ChangeRequestStatus Status,
    DateTimeOffset SubmittedAt,
    int ChangedFieldCount);
