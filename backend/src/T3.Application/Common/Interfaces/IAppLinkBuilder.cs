namespace T3.Application.Common.Interfaces;

/// <summary>
/// Uygulamanın herkese açık taban adresini bilir (yapılandırmadan geliyor,
/// istekteki Host başlığından değil — <see cref="IResetLinkBuilder"/> ile
/// aynı gerekçe: adresi istekten türetmek saldırgana kendi alan adına giden
/// bir bağlantı ürettirmesine izin verirdi). E-posta içindeki "sisteme git"
/// gibi bağlantılar bunu kullanır.
/// </summary>
public interface IAppLinkBuilder
{
    string BuildAppUrl(string relativePath);
}
