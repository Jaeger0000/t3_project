using T3.Application.Features.Assistant.AskAssistant;

namespace T3.Application.Features.Assistant.SummarizeStartup;

/// <summary>
/// Girişim kartının AI özeti. <see cref="Highlights"/> özetin dayanağıdır ve
/// her zaman gönderilir: kaynağı görünmeyen bir özet karar desteği değil,
/// doğrulanamayan bir iddiadır.
/// </summary>
public sealed record StartupSummaryResponse(
    Guid StartupId,
    string Name,
    string Summary,
    IReadOnlyList<string> Highlights,
    AssistantMode Mode,
    string ModelName,
    bool ExactAmountsVisible,
    DateTimeOffset GeneratedAt);
