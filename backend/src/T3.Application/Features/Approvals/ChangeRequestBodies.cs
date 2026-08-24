using T3.Application.Common.Rbac;
using T3.Application.Features.Achievements;
using T3.Application.Features.Documents;
using T3.Application.Features.Startups;
using T3.Application.Features.Startups.Team;
using T3.Domain.Approvals;

namespace T3.Application.Features.Approvals;

/// <summary>
/// Öneri gövdesinin taşınabilir hâli. Kuyruk sorgusu varlığın tamamını değil
/// bu dört alanı çekiyor; onay handler'ı varlığın kendisiyle çalışıyor. İkisi
/// de aynı çözümlemeyi kullanabilsin diye ortak şekil.
/// </summary>
internal sealed record ChangeRequestBody(
    ChangeTargetType TargetType,
    ChangeOperation Operation,
    string PayloadJson,
    string? BeforeJson)
{
    public static ChangeRequestBody Of(ChangeRequest request) =>
        new(request.TargetType, request.Operation, request.PayloadJson, request.BeforeJson);
}

/// <summary>
/// jsonb olarak saklanan öneriyi tipli modele çevirir ve diff üretir. Kuyruk
/// satırı ile onay ekranı aynı çözümlemeden geçmek zorunda: ayrışması,
/// inceleyicinin gördüğü ile onayladığı şeyin farklı olması demek.
/// </summary>
internal static class ChangeRequestBodies
{
    /// <summary>
    /// Kuyruk satırındaki insan tarafından okunur hedef adı. Yeni değer varsa
    /// ondan, yoksa (silme önerisi) eski değerden okunur.
    /// </summary>
    public static string? Subject(ChangeRequestBody body) => body.TargetType switch
    {
        ChangeTargetType.TeamMember =>
            Read<TeamMemberWriteModel>(body.PayloadJson)?.FullName
            ?? Read<TeamMemberWriteModel>(body.BeforeJson)?.FullName,

        ChangeTargetType.Achievement =>
            KindLabel(Read<AchievementWriteModel>(body.PayloadJson))
            ?? KindLabel(Read<AchievementWriteModel>(body.BeforeJson)),

        ChangeTargetType.Document =>
            Read<DocumentProposalModel>(body.PayloadJson)?.FileName
            ?? Read<DocumentProposalModel>(body.BeforeJson)?.FileName,

        _ => null
    };

    private static string? KindLabel(AchievementWriteModel? model) =>
        model is null ? null : AchievementLabels.Kind(model.Kind);

    /// <summary>
    /// Öneriyi alan alan karşılaştırır. Gövde okunamazsa boş liste döner —
    /// çağıran taraf bunu "uygulanamaz öneri" olarak raporlar.
    /// </summary>
    public static IReadOnlyList<DiffFieldResponse> Diff(
        ChangeRequestBody body, StartupVisibility visibility)
    {
        switch (body.TargetType)
        {
            case ChangeTargetType.Startup:
                var afterStartup = Read<StartupWriteModel>(body.PayloadJson);
                return afterStartup is null
                    ? []
                    : ChangeRequestDiff.ForStartup(
                        Read<StartupWriteModel>(body.BeforeJson), afterStartup, visibility);

            case ChangeTargetType.TeamMember:
                var beforeMember = Read<TeamMemberWriteModel>(body.BeforeJson);

                // Silme önerisinde "sonra" tarafı yoktur; gövde boş bırakılır.
                var afterMember = body.Operation == ChangeOperation.Delete
                    ? null
                    : Read<TeamMemberWriteModel>(body.PayloadJson);

                return beforeMember is null && afterMember is null
                    ? []
                    : ChangeRequestDiff.ForTeamMember(beforeMember, afterMember, visibility);

            case ChangeTargetType.Achievement:
                var beforeAchievement = Read<AchievementWriteModel>(body.BeforeJson);

                var afterAchievement = body.Operation == ChangeOperation.Delete
                    ? null
                    : Read<AchievementWriteModel>(body.PayloadJson);

                return beforeAchievement is null && afterAchievement is null
                    ? []
                    : ChangeRequestDiff.ForAchievement(
                        beforeAchievement, afterAchievement, visibility);

            case ChangeTargetType.Document:
                var beforeDocument = Read<DocumentProposalModel>(body.BeforeJson);

                var afterDocument = body.Operation == ChangeOperation.Delete
                    ? null
                    : Read<DocumentProposalModel>(body.PayloadJson);

                return beforeDocument is null && afterDocument is null
                    ? []
                    : ChangeRequestDiff.ForDocument(beforeDocument, afterDocument, visibility);

            default:
                return [];
        }
    }

    public static T? Read<T>(string? json) where T : class =>
        ChangeRequestJson.TryDeserialize<T>(json);
}
