namespace T3.Application.Features.Assistant;

/// <summary>
/// Yanıtın dayandığı tek araç çağrısı.
///
/// <see cref="StartupIds"/> ayrı bir alan: cevap metnindeki girişim adından
/// kimlik çıkarmaya çalışmak hem kırılgan (aynı adlı iki girişim) hem de
/// modelin uydurduğu bir adı gerçek bir kayda bağlama riski taşır. Kimlikler
/// modelden değil, aracın döndürdüğü kayıttan gelir.
///
/// <see cref="DownloadToken"/> yalnızca dosya üreten araçlarda (ör.
/// <c>export_startups_excel</c>) dolu — sohbet protokolü ikili veri
/// taşıyamadığı için model bir jeton görür, istemci jetonu ayrı bir GET ucuna
/// sunar (bkz. IAssistantExportStore).
/// </summary>
public sealed record AssistantSourceResponse(
    string Tool,
    string Summary,
    IReadOnlyList<Guid> StartupIds,
    string? DownloadToken = null,
    string? DownloadFileName = null);

public enum AssistantMode
{
    /// <summary>Dil modeli yanıtladı.</summary>
    Model = 1,

    /// <summary>Anahtar tanımlı değil; yerel anahtar sözcük planlayıcısı yanıtladı.</summary>
    Local = 2
}
