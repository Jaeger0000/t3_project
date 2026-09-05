using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;

namespace T3.Application.Features.Assistant.AskAssistant;

/// <summary>
/// Tek soruluk doğal dil ekosistem sorgusu (karar destek). Bağlam saklamaz:
/// her çağrı kendi başına yanıtlanır. Çok turlu, sunucuda saklanan sohbet için
/// <c>Features/Assistant/Chat</c> dilimine bakın — ikisi de aynı
/// <see cref="AssistantConversationRunner"/> döngüsünü kullanır.
/// </summary>
public sealed class AskAssistantHandler(
    AssistantConversationRunner runner,
    ICurrentUser currentUser,
    IAuditWriter audit)
{
    public async Task<Result<AssistantAnswerResponse>> Handle(
        AskAssistantRequest request, CancellationToken ct)
    {
        // Politika hattı bu ucu zaten kapatıyor; kontrol burada da duruyor
        // çünkü MCP araçları handler'ları doğrudan çağırıyor.
        if (!currentUser.IsAuthenticated)
            return Error.Forbidden("Soru sormak için oturum açmalısınız.");

        var run = await runner.RunAsync(request.Question, [], ct);

        // Sorunun kendisi denetim izine yazılıyor: "AI neyi sordu, hangi veriye
        // dokundu" sorusunun cevabı sonradan da verilebilmeli.
        await audit.WriteAsync(
            "Assistant.Ask", "Assistant", null,
            after: new { request.Question, Tools = run.Sources.Select(s => s.Tool).ToArray() },
            ct: ct);

        return new AssistantAnswerResponse(
            request.Question,
            run.Answer,
            run.Sources,
            run.Mode,
            run.ModelName,
            DateTimeOffset.UtcNow);
    }
}
