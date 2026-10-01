using System.Globalization;
using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Results;
using T3.Application.Features.Achievements.ListAchievements;
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

    /// <summary>
    /// Aynı anda kaç bölüm sorulacağı. Altı bölüm paralel gidiyordu ve bu,
    /// ücretsiz sağlayıcı katmanında raporu düzenli olarak bozan şeydi:
    /// 1 Ekim 2026'da canlıda altı bölümün <b>dördü</b> 429 ile döndü, rapor
    /// "model yanıt veremedi" paragraflarıyla çıktı. Paylaşımlı kovayı tek
    /// seferde doldurmak yerine ikişer ikişer soruyoruz — toplam süre sıralıya
    /// göre yarı, kova açısından ise patlama değil akış.
    /// </summary>
    private const int SectionConcurrency = 2;

    /// <summary>
    /// Model turlarının toplam süre bütçesi. Önündeki nginx 120 sn'de okumayı
    /// bırakıyor; bütçe dolduğunda kalan bölümler modeli hiç beklemeden olgusal
    /// paragrafa düşüyor. Alternatifi, isteğin vekilde 504'e dönmesi ve
    /// kullanıcının hiç rapor alamamasıydı.
    /// </summary>
    private static readonly TimeSpan ModelBudget = TimeSpan.FromSeconds(90);

    /// <summary>
    /// Süzme sonrası bir bölümün basılmaya değer en kısa hâli. Bunun altına
    /// düşen metin yerine kayıtlardan üretilen olgusal özet basılıyor: üç
    /// kelimelik bir bölüm raporu doldurmuyor, bozuk gösteriyor.
    /// </summary>
    private const int MinimumSectionLength = 200;

    private const string SystemPrompt =
        "Sen T3 Girişim Ekosistemi Yönetim Sistemi için girişim raporları yazan bir "
        + "analistsin. Yalnızca sana verilen brife dayanırsın. Yazdığın metin doğrudan "
        + "basılı bir PDF rapora giriyor: teknik terim, alan adı ya da sistem ifadesi "
        + "kullanmazsın.";

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
        //
        // Paket Türkçe düzyazı, JSON değil: ham JSON gönderildiğinde model
        // raporu veritabanı şemasıyla anlatıyordu ("logoUrl alanının null
        // olması"). Kişisel veri de buraya hiç yazılmıyor — AiRedaction'ın
        // alan adı kara listesi yerine beyaz liste (bkz. ReportEvidence).
        var evidence = ReportEvidence.Build(cardValue, timelineValue, achievementsValue);

        // Bütçe: toplam süre dolduğunda kalan bölümler modeli beklemiyor.
        // Bağlantı iptali (ct) ile karıştırılmamalı — bütçe bizim kararımız,
        // iptal kullanıcının.
        using var budget = new CancellationTokenSource(ModelBudget);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, budget.Token);
        using var slot = new SemaphoreSlim(SectionConcurrency, SectionConcurrency);

        var factual = FactualSummary(cardValue);

        var tasks = sections.Select(async section =>
        {
            // Bütçe dolmuşsa sıraya girmeye değmez: her bölümün kendi zaman
            // aşımını beklemesi, raporu 120 sn'lik vekil sınırının ötesine
            // taşıyordu.
            if (budget.IsCancellationRequested)
                return UnavailableSection(factual);

            await slot.WaitAsync(ct);

            try
            {
                return await AskSectionAsync(evidence, section, factual, linked.Token, ct);
            }
            finally
            {
                slot.Release();
            }
        }).ToArray();

        var bodies = await Task.WhenAll(tasks);

        return sections.Zip(bodies, (s, b) => new ReportSectionContent(s.Title, b)).ToList();
    }

    /// <param name="modelCt">
    /// Model çağrısının jetonu: kullanıcının iptaliyle <b>ya da</b> süre
    /// bütçesiyle iptal olur.
    /// </param>
    /// <param name="requestCt">
    /// İsteğin kendi jetonu. İkisini ayırmak gerekiyor: kullanıcı bağlantıyı
    /// kapattıysa rapor üretmenin anlamı yok (hata yukarı gider), bütçe
    /// dolduysa elimizdeki veriyle rapor yine çıkmalı.
    /// </param>
    private async Task<string> AskSectionAsync(
        string evidence,
        ReportSectionDefinition section,
        string factual,
        CancellationToken modelCt,
        CancellationToken requestCt)
    {
        var prompt = $"""
            Aşağıda bir girişimin profilinden, program geçmişinden ve
            başarı/finans kayıtlarından hazırlanmış bir brif var.

            Görevin: {section.Instruction}

            {ReportSections.FormatInstruction}

            Brif:
            {evidence}
            """;

        var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };

        try
        {
            var first = await AskAsync(messages, modelCt);

            if (string.IsNullOrWhiteSpace(first))
                return UnavailableSection(factual);

            if (!ReportProse.HasTechnicalLeak(first, evidence))
                return first;

            // Teknik ifade yakalandı. Bölüm bir kez daha isteniyor; modelin
            // kendi metni de konuşmada duruyor, çünkü "şunu yazdın, böyle
            // olmaz" demek talimatı tekrar etmekten iyi sonuç veriyor.
            // Bütçe dolduysa ikinci tur yok: metin elde var, süzülerek
            // kurtarılabiliyor.
            var answer = first;

            if (!modelCt.IsCancellationRequested)
            {
                messages.Add(new ChatMessage(ChatRole.Assistant, first));
                messages.Add(new ChatMessage(ChatRole.User, ReportSections.RewriteInstruction));

                var second = await AskAsync(messages, modelCt);

                if (!string.IsNullOrWhiteSpace(second))
                {
                    if (!ReportProse.HasTechnicalLeak(second, evidence))
                        return second;

                    answer = second;
                }
            }

            // İkinci deneme de sızdırdı: tüm bölümü çöpe atmak yerine yalnızca
            // sızdıran cümleler atılıyor. Kalan metin basılmaya değmeyecek
            // kadar kısaldıysa olgusal özet geçiyor.
            var cleaned = ReportProse.StripTechnicalSentences(answer!, evidence);

            return cleaned.Length >= MinimumSectionLength ? cleaned : FactualSection(factual);
        }
        catch (Exception ex) when (ex is not OperationCanceledException
                                   || !requestCt.IsCancellationRequested)
        {
            // Bir bölümün hatası (429, zaman aşımı, bütçenin dolması) diğer
            // bölümleri düşürmüyor; o bölüm olgusal paragrafla geçiliyor.
            // Kullanıcı bağlantıyı kapattıysa (requestCt) istisna yukarı gider
            // — orada rapor üretmenin anlamı yok.
            return UnavailableSection(factual);
        }
    }

    /// <summary>
    /// Model turu. Varsayılan <c>AiOptions.MaxTokens</c> (sohbet/özet için
    /// ayarlı) çok paragraflı bir rapor bölümünü ortasında kesiyordu; bu
    /// çağrıya özel daha geniş bir üst sınır veriliyor
    /// (bkz. IChatModel.CompleteAsync).
    /// </summary>
    private async Task<string?> AskAsync(
        IReadOnlyList<ChatMessage> messages, CancellationToken ct)
    {
        var reply = await model.CompleteAsync(SystemPrompt, messages, [], ct, maxTokens: 2000);

        return reply.Text?.Trim();
    }

    /// <summary>
    /// Model o bölümü yanıtlamadığında yazılan metin. Yalnızca özür cümlesi
    /// değil: <paramref name="factual"/> ile birlikte **gerçek sayılar**
    /// gidiyor, böylece rapor her koşulda kullanılabilir bir belge oluyor.
    /// Eski hâli ("birkaç dakika sonra tekrar deneyin") altı bölümün dördünde
    /// göründüğünde raporu kullanılamaz kılıyordu.
    /// </summary>
    private static string UnavailableSection(string factual) =>
        "Bu bölüm için dil modeli yanıt veremedi (sağlayıcı kotası ya da geçici bir hata), "
        + $"bu yüzden yalnızca kayıtlardan üretilen özet gösteriliyor: {factual} "
        + "Raporu birkaç dakika sonra yeniden oluşturmak bu bölümü tamamlar.";

    /// <summary>
    /// Model yanıt verdi ama metni iki denemede de rapor diline uymadı
    /// (bkz. <see cref="ReportProse"/>). Kullanıcıya "model çalışmıyor"
    /// demiyoruz — çalıştı, metni kullanılabilir değildi.
    /// </summary>
    private static string FactualSection(string factual) =>
        "Bu bölümün metni rapor diline uygun hâle getirilemedi, bu yüzden kayıtlardan "
        + $"üretilen özet gösteriliyor: {factual} "
        + "Raporu yeniden oluşturmak bu bölümü tamamlayabilir.";

    /// <summary>
    /// Model yoksa (anahtar tanımsız) rapor yine de boş dönmez — her bölüm
    /// aynı temel olgusal özeti taşır. Demo'da ekran/rapor asla boş kalmamalı.
    /// </summary>
    private static IReadOnlyList<ReportSectionContent> GenerateFallback(
        StartupCardResponse cardValue, IReadOnlyList<ReportSectionDefinition> sections)
    {
        var factual = FactualSummary(cardValue);

        return sections
            .Select(s => new ReportSectionContent(
                s.Title,
                "Bu bölüm şu anda bir dil modeli olmadan üretiliyor, bu yüzden yalnızca "
                + $"temel bir özet gösteriliyor: {factual}"))
            .ToList();
    }

    /// <summary>
    /// Kayıtlardan üretilen, modelsiz de doğru olan tek paragraf. İki yerde
    /// kullanılıyor: anahtar hiç yokken tüm rapor, model bir bölümü
    /// yanıtlamadığında o bölüm.
    /// </summary>
    private static string FactualSummary(StartupCardResponse cardValue) =>
        $"{cardValue.Name}, {StartupLabels.Sector(cardValue.Sector)} sektöründe, "
        + $"{StartupLabels.Status(cardValue.Status).ToLower(Turkish)} durumda bir girişim. "
        + $"{cardValue.Programs.Count} program kaydı, "
        + $"{cardValue.Achievements.TotalCount} başarı/finans kaydı, "
        + $"{cardValue.DocumentCount} doküman bulunuyor.";

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
