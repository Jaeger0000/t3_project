namespace T3.Domain.Achievements;

/// <summary>
/// Dönemsel (yıl/çeyrek) tutar kayıtlarının ortak temeli. Ciro ve ihracat aynı
/// dönem alanlarını paylaştığı için burada bir kez tanımlanır — aksi halde TPH
/// tablosunda her tip için ayrı "FiscalYear" kolonu oluşuyor.
/// </summary>
public abstract class PeriodicMoneyAchievement : MoneyAchievement
{
    /// <summary>Kaydın ait olduğu mali yıl.</summary>
    public int FiscalYear { get; set; }

    /// <summary>1-4 arası çeyrek; yıllık kayıtlarda null.</summary>
    public int? Quarter { get; set; }
}
