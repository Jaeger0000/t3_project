namespace T3.Infrastructure.Notifications;

public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Gönderen adresi; şu an yalnızca kutudaki dosyanın başlığında görünür.</summary>
    public string From { get; set; } = "no-reply@t3ekosistem.test";

    /// <summary>
    /// Bağlantıların işaret ettiği arayüz adresi. Sıfırlama bağlantısı
    /// e-postada gittiği için sunucu istekten değil yapılandırmadan okumak
    /// zorunda: <c>Host</c> başlığına güvenmek bağlantıyı saldırganın
    /// alan adına çevirebilirdi.
    /// </summary>
    public string AppBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>Geliştirme kutusu — gönderilen e-postalar buraya dosya olarak düşer.</summary>
    public string OutboxPath { get; set; } = "storage/outbox";
}
