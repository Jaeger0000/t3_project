using T3.Domain.Assistant;

namespace T3.Application.Features.Assistant.Chat.GetConversation;

public sealed record ConversationResponse(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastMessageAt,
    IReadOnlyList<ConversationMessageResponse> Messages);

/// <summary>
/// Tek tur. Asistan satırı hangi araçlara ve hangi girişimlere dayandığını
/// taşır: geçmişi açan kullanıcı da o günün cevabının kaynağını görebilmeli —
/// kaynağı görünmeyen cümle karar desteği değil, iddiadır.
/// </summary>
/// <param name="DownloadToken">
/// Bu turda bir dosya (ör. Excel dışa aktarma) üretildiyse indirme jetonu.
/// Jeton kısa ömürlü (bkz. IAssistantExportStore) — eski bir sohbette süresi
/// dolmuş olabilir, istemci bunu 404 ile fark eder.
/// </param>
public sealed record ConversationMessageResponse(
    Guid Id,
    AiMessageRole Role,
    string Text,
    IReadOnlyList<Guid> StartupIds,
    IReadOnlyList<string> Tools,
    AssistantMode? Mode,
    string? ModelName,
    DateTimeOffset CreatedAt,
    string? DownloadToken = null,
    string? DownloadFileName = null);
