namespace T3.Application.Features.Startups;

public static class StartupMoney
{
    /// <summary>
    /// Toplamların raporlandığı para birimi. Farklı para birimindeki kayıtlar
    /// kur dönüşümü olmadan toplanmaz; agregatlar yalnızca bu birimi kapsar.
    /// </summary>
    public const string ReportingCurrency = "TRY";
}
