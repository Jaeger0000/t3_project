using FluentValidation;

namespace T3.Application.Features.Assistant.Chat;

/// <summary>
/// Sohbete bir tur ekler. <see cref="ConversationId"/> boşsa yeni sohbet açılır;
/// doluysa bağlam sunucudaki kayıttan gelir — istemci geçmişi kendisi
/// göndermez, çünkü gönderebilseydi hiç sorulmamış bir turu "sorulmuş" gibi
/// sunabilirdi.
/// </summary>
public sealed record ChatRequest(Guid? ConversationId, string Question);

public sealed class ChatValidator : AbstractValidator<ChatRequest>
{
    public ChatValidator()
    {
        // Sınır AskAssistantRequest ile aynı: aynı model, aynı jeton bütçesi.
        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("Soru boş olamaz.")
            .MaximumLength(500).WithMessage("Soru en çok 500 karakter olabilir.");
    }
}

/// <summary>
/// Bir turun yanıtı. <see cref="ConversationId"/> her zaman dönüyor: ilk çağrıda
/// istemcinin sohbeti sürdürebilmesi için tek yol bu.
/// </summary>
public sealed record ChatResponse(
    Guid ConversationId,
    string Answer,
    IReadOnlyList<AssistantSourceResponse> Sources,
    IReadOnlyList<Guid> StartupIds,
    AssistantMode Mode,
    string ModelName,
    DateTimeOffset AnsweredAt);
