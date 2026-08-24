using T3.Domain.Approvals;

namespace T3.Application.Features.Approvals;

/// <summary>Onay kuyruğunun satır başlıkları.</summary>
internal static class ChangeRequestLabels
{
    public static string Operation(ChangeOperation operation) => operation switch
    {
        ChangeOperation.Create => "Ekleme",
        ChangeOperation.Update => "Güncelleme",
        ChangeOperation.Delete => "Çıkarma",
        _ => operation.ToString()
    };

    public static string Target(ChangeTargetType type, string? subject) => type switch
    {
        ChangeTargetType.Startup => "Girişim profili",
        ChangeTargetType.TeamMember => subject is null ? "Ekip üyesi" : $"Ekip üyesi · {subject}",
        ChangeTargetType.Achievement => subject is null ? "Başarı kaydı" : $"Başarı kaydı · {subject}",
        ChangeTargetType.Document => subject is null ? "Doküman" : $"Doküman · {subject}",
        _ => type.ToString()
    };
}
