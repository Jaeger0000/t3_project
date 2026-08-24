using System.Globalization;
using T3.Application.Common.Rbac;
using T3.Application.Features.Achievements;
using T3.Application.Features.Documents;
using T3.Application.Features.Startups;
using T3.Application.Features.Startups.Team;

namespace T3.Application.Features.Approvals;

/// <summary>
/// Onay ekranındaki tek bir "önce / sonra" satırı.
///
/// <paramref name="Masked"/> ile <paramref name="Changed"/> birbirinden
/// bağımsızdır ve bu kasıtlıdır: yetkisiz bir inceleyici vergi numarasının
/// <em>değiştiğini</em> görür, yeni değeri görmez. Kararı verebilmesi için
/// değişikliğin varlığını bilmesi gerekir, içeriğini bilmesi gerekmez.
/// </summary>
public sealed record DiffFieldResponse(
    string Field,
    string Label,
    string? Before,
    string? After,
    bool Changed,
    bool Masked);

/// <summary>
/// Öneri gövdesini alan alan karşılaştırır. Değerler burada Türkçe, gösterime
/// hazır metne çevrilir — bir satır "biçimlenmiş", diğeri "ham" olsaydı arayüz
/// hangi alanın hangi tipte olduğunu bilmek zorunda kalırdı.
/// </summary>
internal static class ChangeRequestDiff
{
    public static IReadOnlyList<DiffFieldResponse> ForStartup(
        StartupWriteModel? before, StartupWriteModel? after, StartupVisibility visibility)
    {
        // Durum alanında null "değişmiyor" demek (bkz. StartupWriteModel.ApplyTo);
        // diff'in de aynı anlamı vermesi gerekir, yoksa formda gönderilmeyen
        // alan "boşaltıldı" gibi görünür.
        var effectiveStatus = after is null ? null : after.Status ?? before?.Status;

        return
        [
            Row("name", "Girişim adı", before?.Name, after?.Name),
            Row("legalName", "Ticari unvan", before?.LegalName, after?.LegalName),
            Row("taxNumber", "Vergi kimlik numarası",
                before?.TaxNumber, after?.TaxNumber, visibility.ShowTaxNumber),
            Row("foundedOn", "Kuruluş tarihi",
                Date(before?.FoundedOn), Date(after?.FoundedOn)),
            Row("sector", "Sektör",
                before is null ? null : StartupLabels.Sector(before.Sector),
                after is null ? null : StartupLabels.Sector(after.Sector)),
            Row("status", "Durum",
                before?.Status is { } b ? StartupLabels.Status(b) : null,
                effectiveStatus is { } a ? StartupLabels.Status(a) : null),
            Row("technologyAreas", "Teknoloji alanları",
                List(before?.TechnologyAreas), List(after?.TechnologyAreas)),
            Row("productDescription", "Ürün açıklaması",
                before?.ProductDescription, after?.ProductDescription),
            Row("website", "Web sitesi", before?.Website, after?.Website),
            Row("logoUrl", "Logo bağlantısı", before?.LogoUrl, after?.LogoUrl),
            Row("city", "Şehir", before?.City, after?.City),
            Row("contactEmail", "İletişim e-postası",
                before?.ContactEmail, after?.ContactEmail, visibility.ShowContactDetails),
            Row("contactPhone", "İletişim telefonu",
                before?.ContactPhone, after?.ContactPhone, visibility.ShowContactDetails)
        ];
    }

    public static IReadOnlyList<DiffFieldResponse> ForTeamMember(
        TeamMemberWriteModel? before, TeamMemberWriteModel? after, StartupVisibility visibility)
    {
        var personal = visibility.ShowTeamPersonalData;

        return
        [
            Row("fullName", "Ad soyad", before?.FullName, after?.FullName),
            Row("title", "Ünvan", before?.Title, after?.Title),
            Row("email", "E-posta", before?.Email, after?.Email, personal),
            Row("phone", "Telefon", before?.Phone, after?.Phone, personal),
            Row("linkedInUrl", "LinkedIn", before?.LinkedInUrl, after?.LinkedInUrl),
            Row("isFounder", "Kurucu",
                before is null ? null : Bool(before.IsFounder),
                after is null ? null : Bool(after.IsFounder)),
            Row("joinedOn", "Katılım tarihi", Date(before?.JoinedOn), Date(after?.JoinedOn))
        ];
    }

    /// <summary>
    /// Başarı/finans kaydı diff'i. Tutar alanları <c>ShowExactAmounts</c>
    /// yetkisine bağlı: inceleyici "tutar değişiyor" bilgisini görür, meblağı
    /// yetkisi varsa görür.
    /// </summary>
    public static IReadOnlyList<DiffFieldResponse> ForAchievement(
        AchievementWriteModel? before, AchievementWriteModel? after, StartupVisibility visibility)
    {
        var amounts = visibility.ShowExactAmounts;

        return
        [
            Row("kind", "Kayıt türü",
                before is null ? null : AchievementLabels.Kind(before.Kind),
                after is null ? null : AchievementLabels.Kind(after.Kind)),
            Row("occurredOn", "Tarih",
                before is null ? null : Date(before.OccurredOn),
                after is null ? null : Date(after.OccurredOn)),
            Row("amount", "Tutar",
                Money(before?.Amount, before?.Currency),
                Money(after?.Amount, after?.Currency), amounts),
            Row("period", "Dönem",
                Period(before?.FiscalYear, before?.Quarter),
                Period(after?.FiscalYear, after?.Quarter)),
            Row("roundType", "Yatırım turu",
                before?.RoundType is { } br ? AchievementLabels.InvestmentRound(br) : null,
                after?.RoundType is { } ar ? AchievementLabels.InvestmentRound(ar) : null),
            Row("valuation", "Değerleme",
                Money(before?.Valuation, before?.Currency),
                Money(after?.Valuation, after?.Currency), amounts),
            Row("investorNames", "Yatırımcılar",
                List(before?.InvestorNames), List(after?.InvestorNames)),
            Row("institution", "Destek veren kurum",
                before?.Institution is { } bi ? AchievementLabels.Institution(bi) : null,
                after?.Institution is { } ai ? AchievementLabels.Institution(ai) : null),
            Row("programName", "Destek programı", before?.ProgramName, after?.ProgramName),
            Row("awardName", "Ödül adı", before?.AwardName, after?.AwardName),
            Row("organization", "Ödülü veren kurum", before?.Organization, after?.Organization),
            Row("rank", "Derece",
                before?.Rank?.ToString(CultureInfo.InvariantCulture),
                after?.Rank?.ToString(CultureInfo.InvariantCulture)),
            Row("targetCountries", "Hedef ülkeler",
                List(before?.TargetCountries), List(after?.TargetCountries)),
            Row("note", "Not", before?.Note, after?.Note)
        ];
    }

    /// <summary>
    /// Doküman diff'i. Dosyanın içeriği onay ekranında gösterilmiyor; karar
    /// üstveriye ve gerekiyorsa indirmeye dayanır. Depo yolu bilinçli olarak
    /// diff'in dışında: iç uygulama ayrıntısı, inceleyicinin kararına katkısı yok.
    /// </summary>
    public static IReadOnlyList<DiffFieldResponse> ForDocument(
        DocumentProposalModel? before, DocumentProposalModel? after, StartupVisibility visibility)
    {
        var documents = visibility.ShowDocuments;

        return
        [
            Row("fileName", "Dosya adı", before?.FileName, after?.FileName, documents),
            Row("type", "Doküman türü",
                before is null ? null : DocumentLabels.Type(before.Type),
                after is null ? null : DocumentLabels.Type(after.Type), documents),
            Row("sizeBytes", "Boyut",
                before is null ? null : DocumentLabels.Size(before.SizeBytes),
                after is null ? null : DocumentLabels.Size(after.SizeBytes), documents)
        ];
    }

    /// <summary>
    /// Değişiklik sayısı maskelemeden bağımsız hesaplanır: kaç alanın
    /// değiştiği inceleyicinin görmeye hakkı olan bilgidir.
    /// </summary>
    public static int ChangedCount(IReadOnlyList<DiffFieldResponse> fields) =>
        fields.Count(f => f.Changed);

    private static DiffFieldResponse Row(
        string field, string label, string? before, string? after, bool authorized = true)
    {
        var changed = !string.Equals(Clean(before), Clean(after), StringComparison.Ordinal);

        return new DiffFieldResponse(
            field,
            label,
            authorized ? Clean(before) : null,
            authorized ? Clean(after) : null,
            changed,
            Masked: !authorized);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // Tarih biçimi kültüre bağlı bırakılmıyor: sunucunun yerel ayarı ne olursa
    // olsun aynı metin üretilsin.
    private static string? Date(DateOnly? value) =>
        value?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    private static string Bool(bool value) => value ? "Evet" : "Hayır";

    /// <summary>
    /// Tutar metni. Kültür açıkça tr-TR: sunucunun yerel ayarına bırakılırsa
    /// aynı öneri iki makinede iki farklı metin üretir ve diff yalan söyler.
    /// </summary>
    private static string? Money(decimal? amount, string? currency) =>
        amount is not { } value
            ? null
            : $"{value.ToString("#,##0.##", Turkish)} {(currency ?? "TRY").ToUpperInvariant()}";

    private static string? Period(int? fiscalYear, int? quarter) =>
        fiscalYear is { } year ? AchievementLabels.Period(year, quarter) : null;

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private static string? List(IReadOnlyList<string>? values) =>
        values is null || values.Count == 0 ? null : string.Join(", ", values);
}
