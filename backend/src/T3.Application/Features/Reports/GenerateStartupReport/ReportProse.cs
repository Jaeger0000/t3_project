using System.Text;
using System.Text.RegularExpressions;

namespace T3.Application.Features.Reports.GenerateStartupReport;

/// <summary>
/// Rapor metninin dilini denetler: basılı bir rapora girmemesi gereken teknik
/// ifadeleri (alan adı, <c>null</c>, veri yapısı terimleri) yakalar.
///
/// Neden sadece talimat yetmiyor: kanıt paketi artık insan diliyle hazırlanıyor
/// (bkz. <see cref="ReportEvidence"/>) ve biçim talimatı teknik dili açıkça
/// yasaklıyor, ama ikisi de olasılık azaltır — garanti vermez. Rapor jüriye ve
/// yöneticiye gidiyor; tek bir "logoUrl alanının null olması" cümlesi belgeyi
/// bitiriyor. Bu yüzden model çıktısı basılmadan önce burada süzülüyor: önce
/// bölüm bir kez daha isteniyor, yine sızıyorsa yalnızca sızdıran cümleler
/// atılıyor.
///
/// Cümle düzeyinde atmak bilinçli bir orta yol: kelimeyi değiştirmek
/// ("null" yerine "boş") Türkçe cümlenin gramerini bozuyor, bölümü tümden
/// atmak ise tek kelime yüzünden sağlam bir analizi çöpe atıyor.
/// </summary>
public static class ReportProse
{
    /// <summary>
    /// Alan adları camelCase geliyor: logoUrl, isVerified, totalExport,
    /// taxNumber, teamPersonalData, contactDetails. Türkçe düzyazıda küçük
    /// harfle başlayıp içinde büyük harf geçen kelime bulunmaz, bu yüzden tek
    /// kalıp hepsini yakalıyor.
    /// </summary>
    private static readonly Regex CamelCaseIdentifier = new(
        @"\b[a-zçğıöşü]+[A-ZÇĞİÖŞÜ][A-Za-zÇĞİÖŞÜçğıöşü0-9]*\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Kalıba uymayan ama rapora girmemesi gereken ifadeler. Liste bilinçli
    /// kısa tutuldu: "alan", "kayıt", "veri" gibi kelimeler Türkçede meşru
    /// ("savunma alanında", "program kaydı"), onları yasaklamak sağlam
    /// cümleleri de attırırdı.
    /// </summary>
    private static readonly string[] ForbiddenPhrases =
    [
        "null", "true", "false", "boolean", "json", "camelcase",
        "veri seti", "veri yapısı", "alan adı", "alanı boş", "alanı eksik",
        "visibility", "achievements", "timeline", "veritabanı",
    ];

    /// <param name="evidence">
    /// Modele verilen kanıt paketi. İçinde geçen bir kelime sızıntı sayılmaz:
    /// kanıt paketinde hiçbir alan adı yok, dolayısıyla orada da görünen bir
    /// kelime şemadan değil gerçek veriden gelmiştir (ör. ürün açıklamasında
    /// yazan "eTicaret" ya da bir yatırımcı adı). Bu, denetimin sağlam
    /// cümleleri atmasını engelliyor.
    /// </param>
    public static bool HasTechnicalLeak(string? text, string? evidence = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        // Kod işareti (`) biçim talimatının dışında: modelin alan adını kod
        // olarak yazdığı ilk belirti buydu.
        if (text.Contains('`') && evidence?.Contains('`') != true) return true;

        foreach (var match in CamelCaseIdentifier.Matches(text).Cast<Match>())
            if (evidence is null || !evidence.Contains(match.Value, StringComparison.Ordinal))
                return true;

        var lower = text.ToLowerInvariant();
        var lowerEvidence = evidence?.ToLowerInvariant();

        return ForbiddenPhrases.Any(phrase =>
            ContainsWord(lower, phrase)
            && (lowerEvidence is null || !ContainsWord(lowerEvidence, phrase)));
    }

    /// <summary>
    /// Sızdıran cümleleri atar, kalanı olduğu gibi döndürür. Madde imi
    /// satırları tek birim sayılıyor: yarısı atılmış bir madde imi okunmaz.
    /// </summary>
    public static string StripTechnicalSentences(string text, string? evidence = null)
    {
        var kept = new StringBuilder();

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            if (string.IsNullOrWhiteSpace(line))
            {
                kept.AppendLine();
                continue;
            }

            if (line.TrimStart().StartsWith("- ", StringComparison.Ordinal))
            {
                if (!HasTechnicalLeak(line, evidence)) kept.AppendLine(line);
                continue;
            }

            var clean = string.Join(" ", SplitSentences(line)
                .Where(sentence => !HasTechnicalLeak(sentence, evidence)));

            if (!string.IsNullOrWhiteSpace(clean)) kept.AppendLine(clean);
        }

        // Atılan cümlelerin bıraktığı üst üste boş satırlar PDF'te kocaman
        // boşluk olarak basılıyor.
        return Regex.Replace(kept.ToString().Trim(), @"\n{3,}", "\n\n");
    }

    /// <summary>
    /// Cümle sonu: nokta/ünlem/soru işareti + boşluk. Türkçe kısaltma ve
    /// ondalık ayırıcı yüzünden tam bir ayrıştırıcı değil, burada yeterli:
    /// yanlış bölünen cümle en kötü ikiye ayrılıp yine basılıyor.
    /// </summary>
    private static IEnumerable<string> SplitSentences(string line) =>
        Regex.Split(line, @"(?<=[.!?])\s+")
            .Where(sentence => !string.IsNullOrWhiteSpace(sentence))
            .Select(sentence => sentence.Trim());

    /// <summary>
    /// Kelime sınırına bakan arama: "null" yakalanmalı, başka bir kelimenin
    /// içinde geçen aynı harf dizisi yakalanmamalı.
    /// </summary>
    private static bool ContainsWord(string haystack, string needle)
    {
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);

        while (index >= 0)
        {
            var beforeOk = index == 0 || !char.IsLetterOrDigit(haystack[index - 1]);
            var after = index + needle.Length;
            var afterOk = after >= haystack.Length || !char.IsLetterOrDigit(haystack[after]);

            if (beforeOk && afterOk) return true;

            index = haystack.IndexOf(needle, index + 1, StringComparison.Ordinal);
        }

        return false;
    }
}
