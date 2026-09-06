using System.Net;

namespace T3.Application.Features.Notifications;

/// <summary>
/// Bildirim e-postasının içeriği. Şablon burada (Application) duruyor,
/// SMTP/dosya kutusu Infrastructure'da yalnızca gönderiyor — böylece
/// markanın nasıl göründüğü sağlayıcıya bağlı olmuyor.
///
/// HTML gövdeye giren her serbest metin alanı (gönderen adı, girişim adı,
/// yöneticinin yazdığı mesaj) <see cref="WebUtility.HtmlEncode"/>'dan
/// geçiyor: bu bir web sayfası değil ama HTML e-posta istemcisi HTML'i
/// render ediyor, kaçırılmamış bir mesaj gövdesi e-postaya rastgele HTML
/// (ör. sahte bir bağlantı) enjekte etmenin yolu olurdu.
/// </summary>
public static class NotificationEmailTemplate
{
    private const string BrandColor = "#E73A13";

    public static string Subject(string startupName) =>
        $"T3 Girişim Ekosistemi — {startupName} için yeni bildirim";

    public static string PlainText(
        string startupName, string senderName, string senderRoleLabel, string message, string appUrl) =>
        $"""
        Merhaba,

        {senderName} ({senderRoleLabel}) {startupName} için size yeni bir bildirim gönderdi:

        "{message}"

        Bildirimlerinizi görüntülemek ve girişim kaydınıza gitmek için sisteme giriş yapın:
        {appUrl}

        --
        T3 Vakfı Girişim Ekosistemi Yönetim Sistemi
        Bu e-posta otomatik olarak gönderilmiştir, lütfen doğrudan yanıtlamayın.
        """;

    public static string Html(
        string startupName, string senderName, string senderRoleLabel, string message, string appUrl)
    {
        var safeStartup = WebUtility.HtmlEncode(startupName);
        var safeSender = WebUtility.HtmlEncode(senderName);
        var safeRole = WebUtility.HtmlEncode(senderRoleLabel);
        var safeMessage = WebUtility.HtmlEncode(message).Replace("\n", "<br/>");
        var safeUrl = WebUtility.HtmlEncode(appUrl);

        return $"""
            <!doctype html>
            <html lang="tr">
            <head><meta charset="utf-8"/><meta name="viewport" content="width=device-width, initial-scale=1"/></head>
            <body style="margin:0;padding:24px;background:#f5f5f4;font-family:Arial,Helvetica,sans-serif;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;margin:0 auto;">
                <tr>
                  <td style="background:{BrandColor};border-radius:10px 10px 0 0;padding:24px 32px;">
                    <span style="color:#ffffff;font-size:20px;font-weight:bold;">T3 Vakfı</span>
                    <div style="color:#ffe5de;font-size:13px;margin-top:4px;">Girişim Ekosistemi Yönetim Sistemi</div>
                  </td>
                </tr>
                <tr>
                  <td style="background:#ffffff;border:1px solid #e7e5e4;border-top:none;border-radius:0 0 10px 10px;padding:32px;">
                    <h1 style="font-size:17px;margin:0 0 16px;color:#1c1917;">{safeStartup} için yeni bir bildirim var</h1>
                    <p style="font-size:14px;line-height:1.6;color:#44403c;margin:0 0 12px;">
                      <strong>{safeSender}</strong> ({safeRole}) size şu bildirimi gönderdi:
                    </p>
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 20px;">
                      <tr>
                        <td style="background:#fafaf9;border-left:3px solid {BrandColor};padding:16px;font-size:14px;line-height:1.6;color:#1c1917;">
                          {safeMessage}
                        </td>
                      </tr>
                    </table>
                    <a href="{safeUrl}" style="display:inline-block;background:{BrandColor};color:#ffffff;text-decoration:none;padding:10px 22px;border-radius:6px;font-size:14px;font-weight:600;">
                      Sisteme git
                    </a>
                  </td>
                </tr>
                <tr>
                  <td style="padding:16px 8px;text-align:center;">
                    <span style="font-size:11px;color:#a8a29e;">
                      T3 Vakfı Girişim Ekosistemi Yönetim Sistemi tarafından otomatik gönderilmiştir.
                    </span>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    /// <summary>Program Yöneticisi'nin gönderdiği bildirimin SuperAdmin'e giden kopyası.</summary>
    public static string CcSubject(string subject) => $"[Program Yöneticisi bildirimi] {subject}";
}
