using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Features.Achievements.ListAchievements;
using T3.Application.Features.Assistant;
using T3.Application.Features.Startups;
using T3.Application.Features.Startups.GetStartupCard;
using T3.Application.Features.Startups.GetStartupTimeline;
using T3.Application.Features.Users;
using T3.Domain.Identity;

namespace T3.Application.Features.Reports.GenerateStartupReport;

/// <summary>
/// Tek bir girişim için "agentic" AI raporu üretir (MVP'nin dışında,
/// karar destek eklentisi). Kullanıcının isteği tek bir toplu promptla değil,
/// her bölüm için AYRI bir model çağrısıyla karşılanır — model her seferinde
/// TÜM veriyi görür ama yalnızca o bölümün sorusuna odaklanır. PDF şablonu
/// (marka, sayfa düzeni) <see cref="IReportPdfRenderer"/> tarafında sabit
/// kodlu; burada yalnızca içerik üretilir.
///
/// Erişim SuperAdmin/ProgramManager ile sınırlı — aynı "karar destek" katmanı
/// (ekosistem karnesi, onay kuyruğu incelemesi) bu iki role açık, girişim
/// kullanıcısının kendi verisi hakkında bir AI raporu istemesi kapsam dışı.
/// </summary>
public sealed class GenerateStartupReportHandler(
    IAppDbContext db,
    GetStartupCardHandler card,
    GetStartupTimelineHandler timeline,
    ListAchievementsHandler achievements,
    IChatModel model,
    IReportPdfRenderer renderer,
    ICurrentUser currentUser)
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public async Task<Result<PdfFile>> Handle(
        Guid startupId, GenerateStartupReportRequest request, CancellationToken ct)
    {
        // Handler'daki tekrar kontrol bilinçli: uç noktadaki politika MCP
        // üzerinden doğrudan çağrıldığında atlanabilir.
        if (currentUser.Role is not (UserRole.SuperAdmin or UserRole.ProgramManager))
            return Error.Forbidden("AI raporu oluşturma yetkiniz yok.");

        if (request.CustomFocus is { Length: > 1000 })
            return Error.Validation("Özel istek en fazla 1000 karakter olabilir.");

        // Kapsam kontrolü card.Handle içinde: kapsam dışı girişim NotFound
        // döner (bkz. GetStartupCardHandler), burada tekrar edilmiyor.
        var cardResult = await card.Handle(startupId, ct);
        if (!cardResult.IsSuccess) return cardResult.Error!;

        var timelineResult = await timeline.Handle(startupId, ct);
        if (!timelineResult.IsSuccess) return timelineResult.Error!;

        var achievementsResult = await achievements.Handle(startupId, kind: null, ct);
        if (!achievementsResult.IsSuccess) return achievementsResult.Error!;

        var cardValue = cardResult.Value!;

        var sections = ResolveSections(request);
        if (sections.Count == 0)
            return Error.Validation("Oluşturulacak en az bir bölüm ya da özel istek belirtilmeli.");

        var contents = model.IsAvailable
            ? await GenerateWithModelAsync(
                cardValue, timelineResult.Value!, achievementsResult.Value!, sections, ct)
            : GenerateFallback(cardValue, sections);

        var generatedByName = await db.Users.AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(ct) ?? "—";

        var document = new StartupReportDocument(
            cardValue.Name,
            StartupLabels.Sector(cardValue.Sector),
            cardValue.City,
            StartupLabels.Status(cardValue.Status),
            DateTimeOffset.UtcNow,
            generatedByName,
            UserLabels.Role(currentUser.Role!.Value),
            contents);

        var bytes = renderer.Render(document);
        var fileName = $"t3-{Slugify(cardValue.Name)}-ai-rapor-{DateTimeOffset.UtcNow:yyyyMMdd-HHmm}.pdf";

        return new PdfFile(fileName, "application/pdf", bytes);
    }

    /// <summary>
    /// İstenen bölüm kümesini çözer. İkisi de boşsa (bölüm seçilmemiş, özel
    /// istek de yok) tam rapor üretilir — bu varsayılan davranış. Yalnızca
    /// özel istek verilip bölüm seçilmemişse SADECE o tek bölüm üretilir
    /// (ör. "büyüme önerisi hazırla" gibi doğrudan bir istek).
    /// </summary>
    private static List<ReportSectionDefinition> ResolveSections(GenerateStartupReportRequest request)
    {
        var customFocus = request.CustomFocus?.Trim();
        var hasCustom = !string.IsNullOrEmpty(customFocus);
        var keys = request.Sections?.Where(k => !string.IsNullOrWhiteSpace(k)).ToArray() ?? [];

        var sections = new List<ReportSectionDefinition>();

        if (keys.Length == 0 && !hasCustom)
            sections.AddRange(ReportSections.Standard);
        else
            foreach (var key in keys)
                if (ReportSections.Find(key) is { } found)
                    sections.Add(found);

        if (hasCustom)
            sections.Add(new ReportSectionDefinition(
                "OzelIstek", "Özel İstek",
                $"Kullanıcının özel isteği şu: \"{customFocus}\". Yalnızca bu isteğe, "
                + "aşağıda verilen veriye dayanarak yanıt ver."));

        return sections;
    }

    private async Task<IReadOnlyList<ReportSectionContent>> GenerateWithModelAsync(
        StartupCardResponse cardValue,
        StartupTimelineResponse timelineValue,
        AchievementListResponse achievementsValue,
        IReadOnlyList<ReportSectionDefinition> sections,
        CancellationToken ct)
    {
        // Tek bir kanıt paketi, her bölüme aynen gidiyor — "her başlık tüm
        // veriyi kullanarak ayrı cevap versin" isteği tam olarak bu demek.
        // Dış sağlayıcıya (OpenRouter) gitmeden önce AiRedaction'dan geçiyor:
        // REST'te görme yetkisi olan bir alanı (ör. ekip e-postası) üçüncü
        // tarafa aktarmayı kabul etmiş sayılmıyoruz (bkz. AiRedaction.cs).
        var evidenceJson = AiRedaction.Redact(JsonSerializer.Serialize(
            new { card = cardValue, timeline = timelineValue, achievements = achievementsValue },
            JsonOptions));

        // Bölümler paralel üretiliyor: sıralı 6 model turu demo sırasında
        // kullanıcıyı dakikalarca bekletirdi.
        var tasks = sections.Select(section => AskSectionAsync(evidenceJson, section, ct)).ToArray();
        var bodies = await Task.WhenAll(tasks);

        return sections.Zip(bodies, (s, b) => new ReportSectionContent(s.Title, b)).ToList();
    }

    private async Task<string> AskSectionAsync(
        string evidenceJson, ReportSectionDefinition section, CancellationToken ct)
    {
        var prompt = $"""
            Aşağıda bir girişimin profil bilgisi, program/gelişim geçmişi ve
            başarı/finans kayıtları JSON olarak veriliyor.

            Görevin: {section.Instruction}

            {ReportSections.FormatInstruction}

            Veri:
            {evidenceJson}
            """;

        try
        {
            // Varsayılan AiOptions.MaxTokens (sohbet/özet için ayarlı) çok
            // paragraflı bir rapor bölümünü ortasında kesiyordu; bu çağrıya
            // özel daha geniş bir üst sınır veriliyor (bkz. IChatModel.CompleteAsync).
            var reply = await model.CompleteAsync(
                "Sen T3 Girişim Ekosistemi Yönetim Sistemi için ayrıntılı, yalnızca "
                + "verilen veriye dayalı büyüme raporu bölümleri yazan bir asistansın.",
                [new ChatMessage(ChatRole.User, prompt)],
                [],
                ct,
                maxTokens: 2000);

            return string.IsNullOrWhiteSpace(reply.Text)
                ? "Bu bölüm için model şu anda bir yanıt üretemedi."
                : reply.Text!;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            // Bölümler paralel gidiyor: ücretsiz modelde biri zaman aşımına
            // uğrasa/hata dönse bile diğer beş bölümün başarılı sonucunu
            // tümden kaybetmemek için hata burada yutuluyor, yalnızca bu
            // bölüm bilgilendirici bir metinle geçiliyor.
            return "Bu bölüm için model şu anda yanıt veremedi (bağlantı zaman aşımı ya da "
                + "geçici bir hata). Raporu birkaç dakika sonra yeniden oluşturmayı deneyin.";
        }
    }

    /// <summary>
    /// Model yoksa (anahtar tanımsız) rapor yine de boş dönmez — her bölüm
    /// aynı temel olgusal özeti taşır. Demo'da ekran/rapor asla boş kalmamalı.
    /// </summary>
    private static IReadOnlyList<ReportSectionContent> GenerateFallback(
        StartupCardResponse cardValue, IReadOnlyList<ReportSectionDefinition> sections)
    {
        var factual =
            $"{cardValue.Name}, {StartupLabels.Sector(cardValue.Sector)} sektöründe, "
            + $"{StartupLabels.Status(cardValue.Status).ToLower(Turkish)} durumda bir girişim. "
            + $"{cardValue.Programs.Count} program kaydı, "
            + $"{cardValue.Achievements.TotalCount} başarı/finans kaydı, "
            + $"{cardValue.DocumentCount} doküman bulunuyor.";

        return sections
            .Select(s => new ReportSectionContent(
                s.Title,
                "Bu bölüm şu anda bir dil modeli olmadan üretiliyor, bu yüzden yalnızca "
                + $"temel bir özet gösteriliyor: {factual}"))
            .ToList();
    }

    private static string Slugify(string name)
    {
        var normalized = name.Trim().ToLower(Turkish)
            .Replace('ı', 'i').Replace('ğ', 'g').Replace('ü', 'u')
            .Replace('ş', 's').Replace('ö', 'o').Replace('ç', 'c');

        var chars = normalized.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);

        while (slug.Contains("--")) slug = slug.Replace("--", "-");

        return slug.Trim('-') is { Length: > 0 } trimmed ? trimmed : "girisim";
    }
}
