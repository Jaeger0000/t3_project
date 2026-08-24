namespace T3.Application.Common.Text;

/// <summary>
/// E-postayı denetim izine yazılabilir hâle getirir.
///
/// Başarısız giriş denemesi kişisel veri üretir: iz, saldırganın denediği ham
/// adreslerin listesine dönüşürse kendisi bir sızıntı kaynağı olur. Alan adı
/// korunuyor çünkü güvenlik incelemesinin sorusu "hangi kurumdan deniyorlar";
/// yerel kısımdan yalnızca ilk harf kalıyor çünkü kişiyi tanımlamaya yeter
/// bilgi izin amacı değil.
/// </summary>
public static class MaskedEmail
{
    public static string Of(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "(boş)";

        var trimmed = email.Trim();
        var at = trimmed.IndexOf('@');

        if (at <= 0)
            return "***";

        var local = trimmed[..at];
        var domain = trimmed[(at + 1)..];

        return $"{local[0]}***@{domain}";
    }
}
