namespace T3.Domain.Achievements;

/// <summary>
/// Tutar taşıyan başarı kayıtlarının ortak temeli. Tutar alanları hassas veri
/// sayılır: Karar Verici rolüne yalnızca agregat olarak sunulur.
/// </summary>
public abstract class MoneyAchievement : Achievement
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
}
