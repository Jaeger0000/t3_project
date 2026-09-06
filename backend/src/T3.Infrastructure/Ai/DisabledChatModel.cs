using T3.Application.Common.Interfaces;

namespace T3.Infrastructure.Ai;

/// <summary>
/// Anahtar tanımlı değilken kayıtlı olan model. Çağrılmaz — çağıran taraf
/// <see cref="IsAvailable"/> bayrağına bakar. Yine de kayıtlı olması gerekiyor:
/// DI'da "yok" durumu, her handler'da null kontrolü demek olurdu.
/// </summary>
public sealed class DisabledChatModel : IChatModel
{
    public bool IsAvailable => false;

    public string Name => "devre dışı";

    public Task<ChatTurn> CompleteAsync(
        string systemPrompt,
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ChatTool> tools,
        CancellationToken ct,
        int? maxTokens = null) =>
        throw new InvalidOperationException(
            "Dil modeli yapılandırılmadı. Çağırmadan önce IsAvailable kontrol edilmeli.");
}
