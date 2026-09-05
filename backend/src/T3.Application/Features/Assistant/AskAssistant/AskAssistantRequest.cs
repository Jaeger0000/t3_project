using FluentValidation;

namespace T3.Application.Features.Assistant.AskAssistant;

public sealed record AskAssistantRequest(string Question);

public sealed class AskAssistantValidator : AbstractValidator<AskAssistantRequest>
{
    public AskAssistantValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("Soru boş olamaz.")
            .MaximumLength(500).WithMessage("Soru en çok 500 karakter olabilir.");
    }
}

/// <summary>
/// Yanıt her zaman hangi kayıtlara dayandığını taşır (<see cref="Sources"/>).
/// AI karar verici değil karar destek katmanı: kaynağı görünmeyen bir cümle
/// jüriye de kullanıcıya da doğrulanabilir bir şey söylemez.
/// </summary>
public sealed record AssistantAnswerResponse(
    string Question,
    string Answer,
    IReadOnlyList<AssistantSourceResponse> Sources,
    AssistantMode Mode,
    string ModelName,
    DateTimeOffset AnsweredAt);

// AssistantSourceResponse ve AssistantMode alanın ortak dosyasına taşındı
// (Features/Assistant/AssistantResponse.cs): tek soru ucu, kalıcı sohbet ve
// girişim özeti dilimlerinin üçü de aynı tipi döndürüyor; birinin klasöründe
// tutmak diğer ikisini o dilime bağımlı kılardı.
