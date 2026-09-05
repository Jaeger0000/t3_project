namespace T3.Infrastructure.Ai;

/// <summary>
/// Sağlayıcıdan öğrenilen yetenekler. Şu an tek bilgi taşıyor: model araç
/// çağırmayı reddetti mi.
///
/// Neden süreç ömrü boyunca hatırlanıyor: araç desteği olmayan bir modelde her
/// istek önce araçlı denenip reddedilseydi, her soru iki API çağrısı ve iki kat
/// gecikme demek olurdu. İlk redden sonra doğrudan araçsız yola giriliyor.
/// Bayrak yalnızca ileri yönde (true'ya) döner; model/ayar değişince süreç
/// yeniden başlatılıyor zaten.
/// </summary>
public sealed class AiCapabilityState
{
    public bool ToolsDisabled { get; private set; }

    public void DisableTools() => ToolsDisabled = true;
}
