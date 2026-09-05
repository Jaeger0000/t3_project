using T3.Domain.Identity;

namespace T3.Application.Common.Interfaces;

/// <summary>Jetondaki claim'lerle karşılaştırılacak canlı kullanıcı durumu.</summary>
public sealed record UserState(bool IsActive, UserRole Role, Guid SecurityStamp, bool MustChangePassword);

/// <summary>
/// Jetonun içindeki kimlik/rol/durum bilgisini her istekte veritabanındaki
/// güncel değerle karşılaştırmak için kullanılır (bkz. G-01,
/// Guvenlik_Denetimi_ve_Iyilestirme_Plani.md). Her istekte sorgu atmamak için
/// kısa süreli önbelleklenir — "yetkiyi geri alma" bu yüzden anlık değil,
/// önbellek penceresi kadar (birkaç saniye) gecikmelidir.
/// </summary>
public interface IUserStateProvider
{
    Task<UserState?> GetAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Kullanıcının durumu değiştiğinde önbelleği hemen boşaltır.</summary>
    void Invalidate(Guid userId);
}
