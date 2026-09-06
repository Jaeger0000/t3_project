using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Assistant;

namespace T3.Application.Features.Assistant.Chat.GetConversation;

/// <summary>
/// Tek sohbet ve turları. Sahibi olmayan için 404 — bkz. <see cref="ChatHandler"/>:
/// "yasak" demek, kimliğin var olduğunu doğrulamak olurdu.
/// </summary>
public sealed class GetConversationHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<ConversationResponse>> Handle(Guid id, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
            return Error.Forbidden("Sohbet geçmişi için oturum açmalısınız.");

        var conversation = await db.AiConversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(ConversationAccess.Owned(id, userId), ct);

        if (conversation is null)
            return Error.NotFound("Sohbet bulunamadı.");

        // Sıralama ChatHistory ile aynı kuralı izliyor: aynı ana düşen soru ve
        // cevap rolüne göre ayrışsın.
        var messages = conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Role)
            .Select(ToResponse)
            .ToList();

        return new ConversationResponse(
            conversation.Id, conversation.Title, conversation.CreatedAt,
            conversation.LastMessageAt, messages);
    }

    /// <summary>
    /// Eşleme elle yazılıyor (mapper yok): saklanan mod metni burada tekrar
    /// enum'a dönüyor, çözülemezse alan boş kalıyor — eski bir satır yüzünden
    /// tüm sohbetin okunamaz olması kabul edilebilir değil.
    /// </summary>
    private static ConversationMessageResponse ToResponse(AiConversationMessage message) =>
        new(message.Id,
            message.Role,
            message.Text,
            Ids(message.StartupIdsJson),
            Names(message.ToolNamesJson),
            Enum.TryParse<AssistantMode>(message.Mode, out var mode) ? mode : null,
            message.ModelName,
            message.CreatedAt,
            message.ExportDownloadToken,
            message.ExportFileName);

    private static IReadOnlyList<Guid> Ids(string? json) =>
        Parse<Guid>(json);

    private static IReadOnlyList<string> Names(string? json) =>
        Parse<string>(json);

    private static IReadOnlyList<T> Parse<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
