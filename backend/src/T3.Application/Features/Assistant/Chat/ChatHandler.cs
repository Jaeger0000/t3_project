using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Domain.Assistant;

namespace T3.Application.Features.Assistant.Chat;

/// <summary>
/// Çok turlu sohbetin tek yazma ucu. Bağlam sunucuda: istemci yalnızca sohbet
/// kimliğini ve yeni soruyu gönderir.
/// </summary>
public sealed class ChatHandler(
    IAppDbContext db,
    AssistantConversationRunner runner,
    ICurrentUser currentUser,
    IAuditWriter audit)
{
    /// <summary>Başlık ilk sorudan türetilir; liste ekranında okunabilir kalsın diye kısa.</summary>
    private const int TitleLength = 80;

    /// <summary>Mesaj metni sütunu 8000 karakter; taşan yanıt tüm turu düşürmesin.</summary>
    private const int MaxTextLength = 8000;

    /// <summary>
    /// JSON kimlik listesi sütunu 2000 karakter. Bir GUID ~39 karakter yer
    /// tuttuğu için 40 kimlik sığar; geniş bir aramada fazlası kırpılır —
    /// saklanan liste geçmişi yeniden gösterirken kullanılıyor, yanıtın kendisi
    /// tam listeyi taşıyor.
    /// </summary>
    private const int MaxStoredStartupIds = 40;

    public async Task<Result<ChatResponse>> Handle(ChatRequest request, CancellationToken ct)
    {
        // Politika hattı bu ucu zaten kapatıyor; kontrol burada da duruyor
        // çünkü handler'lar MCP üzerinden politika hattı olmadan da çağrılabilir.
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
            return Error.Forbidden("Sohbet için oturum açmalısınız.");

        AiConversation conversation;
        IReadOnlyList<ChatMessage> history = [];

        if (request.ConversationId is { } conversationId)
        {
            var existing = await db.AiConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(ConversationAccess.Owned(conversationId, userId), ct);

            // Sahiplik süzgeci sorgunun içinde ve hata 404: "var ama senin
            // değil" demek, başkasının sohbet kimliğini doğrulamak olurdu.
            if (existing is null)
                return Error.NotFound("Sohbet bulunamadı.");

            conversation = existing;
            history = ChatHistory.Build(existing.Messages);
        }
        else
        {
            conversation = new AiConversation
            {
                OwnerUserId = userId,
                Title = Title(request.Question)
            };

            db.AiConversations.Add(conversation);
        }

        var run = await runner.RunAsync(request.Question, history, ct);

        var askedAt = DateTimeOffset.UtcNow;
        conversation.LastMessageAt = askedAt;

        db.AiConversationMessages.Add(new AiConversationMessage
        {
            Conversation = conversation,
            Role = AiMessageRole.User,
            Text = Truncate(request.Question),
            CreatedAt = askedAt
        });

        db.AiConversationMessages.Add(new AiConversationMessage
        {
            Conversation = conversation,
            Role = AiMessageRole.Assistant,
            Text = Truncate(run.Answer),
            ToolNamesJson = JsonSerializer.Serialize(
                run.Sources.Select(s => s.Tool).Distinct().ToArray()),
            StartupIdsJson = JsonSerializer.Serialize(
                run.StartupIds.Take(MaxStoredStartupIds).ToArray()),
            Mode = run.Mode.ToString(),
            ModelName = run.ModelName,
            CreatedAt = askedAt
        });

        // Soru metni denetim izine yazılmıyor. Tek soru ucu (Assistant.Ask)
        // soruyu yazıyor ama orada soru hiçbir yerde saklanmıyor; burada aynı
        // metin zaten sohbet tablosunda duruyor ve KVKK saklama süresi (1 yıl)
        // ona göre işliyor. İzin 10 yıl saklanan satırına kopyalasaydık serbest
        // metni sohbetin saklama süresinden bağımsız hâle getirirdik. İz "kim,
        // hangi sohbette, hangi araçlarla hangi girişimlere dokundu" sorusunu
        // yanıtlamaya yetiyor.
        await audit.WriteAsync(
            "Assistant.Chat", "AiConversation", conversation.Id,
            after: new
            {
                Tools = run.Sources.Select(s => s.Tool).Distinct().ToArray(),
                StartupIds = run.StartupIds,
                Mode = run.Mode.ToString()
            },
            ct: ct,
            // Tek SaveChanges: sohbet turu ile izi ayrı yazmak, biri düşerse
            // ikisini birbirinden koparırdı (bkz. G-09).
            saveChanges: false);

        await db.SaveChangesAsync(ct);

        return new ChatResponse(
            conversation.Id,
            run.Answer,
            run.Sources,
            run.StartupIds,
            run.Mode,
            run.ModelName,
            askedAt);
    }

    /// <summary>Sohbet başlığı — ilk sorunun baş tarafı.</summary>
    public static string Title(string question)
    {
        var text = question.Trim();
        return text.Length <= TitleLength ? text : text[..TitleLength].TrimEnd() + "…";
    }

    private static string Truncate(string text) =>
        text.Length <= MaxTextLength ? text : text[..MaxTextLength];
}
