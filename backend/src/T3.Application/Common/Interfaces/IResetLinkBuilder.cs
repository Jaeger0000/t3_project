namespace T3.Application.Common.Interfaces;

/// <summary>
/// Şifre sıfırlama bağlantısını kurar. Ayrı arayüz olmasının nedeni adresin
/// yapılandırmadan gelmesi: bağlantıyı istekteki <c>Host</c> başlığından
/// türetmek, saldırganın kendi alan adına giden bir sıfırlama bağlantısı
/// ürettirmesine izin verirdi.
/// </summary>
public interface IResetLinkBuilder
{
    string Build(string rawToken);
}
