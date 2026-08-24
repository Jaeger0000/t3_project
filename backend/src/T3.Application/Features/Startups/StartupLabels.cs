using T3.Domain.Startups;

namespace T3.Application.Features.Startups;

/// <summary>
/// Sektör ve durum enum'larının Türkçe karşılıkları. Onay ekranındaki
/// "önce / sonra" karşılaştırması alan değerlerini hazır metin olarak
/// gönderir: diff satırı hangi alanın hangi tipte olduğunu taşımadığı için
/// arayüzün etiket tablosuna bakması mümkün değil.
/// </summary>
public static class StartupLabels
{
    public static string Sector(Sector sector) => sector switch
    {
        Domain.Startups.Sector.Defense => "Savunma",
        Domain.Startups.Sector.Health => "Sağlık",
        Domain.Startups.Sector.Software => "Yazılım",
        Domain.Startups.Sector.Energy => "Enerji",
        Domain.Startups.Sector.Agriculture => "Tarım",
        Domain.Startups.Sector.Education => "Eğitim",
        Domain.Startups.Sector.Finance => "Finans",
        Domain.Startups.Sector.Mobility => "Mobilite",
        Domain.Startups.Sector.Space => "Uzay",
        Domain.Startups.Sector.Manufacturing => "Üretim",
        _ => "Diğer"
    };

    public static string Status(StartupStatus status) => status switch
    {
        StartupStatus.Active => "Faal",
        StartupStatus.Inactive => "Pasif",
        StartupStatus.Graduated => "Mezun",
        StartupStatus.Exited => "Çıkış yaptı",
        StartupStatus.Acquired => "Satın alındı",
        _ => status.ToString()
    };
}
