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
public sealed record ConversationMessageResponse(
    Guid Id,
    AiMessageRole Role,
    string Text,
    IReadOnlyList<Guid> StartupIds,
    IReadOnlyList<string> Tools,
    AssistantMode? Mode,
    string? ModelName,
    DateTimeOffset CreatedAt);
