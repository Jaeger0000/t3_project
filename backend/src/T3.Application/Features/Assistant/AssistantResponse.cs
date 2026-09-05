namespace T3.Application.Features.Assistant;

/// <summary>
/// Yanıtın dayandığı tek araç çağrısı.
///
/// <see cref="StartupIds"/> ayrı bir alan: cevap metnindeki girişim adından
/// kimlik çıkarmaya çalışmak hem kırılgan (aynı adlı iki girişim) hem de
/// modelin uydurduğu bir adı gerçek bir kayda bağlama riski taşır. Kimlikler
/// modelden değil, aracın döndürdüğü kayıttan gelir.
/// </summary>
public sealed record AssistantSourceResponse(
    string Tool,
    string Summary,
    IReadOnlyList<Guid> StartupIds);

public enum AssistantMode
{
    /// <summary>Dil modeli yanıtladı.</summary>
    Model = 1,

    /// <summary>Anahtar tanımlı değil; yerel anahtar sözcük planlayıcısı yanıtladı.</summary>
    Local = 2
}
