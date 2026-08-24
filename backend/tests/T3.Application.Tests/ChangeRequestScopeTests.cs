using T3.Application.Common.Rbac;
using T3.Application.Features.Approvals;
using T3.Domain.Approvals;
using T3.Domain.Identity;

namespace T3.Application.Tests;

/// <summary>
/// Onay akışının satır düzeyi yetkilendirmesi. Kuyruk hem inceleyicinin iş
/// listesi hem girişimin kendi geçmişi olduğu için tek hata iki yönde sızdırır:
/// bir girişim başkasının önerisini görebilir ya da inceleyici kapsamı dışına
/// çıkar.
/// </summary>
public class ChangeRequestScopeTests
{
    private static readonly Guid OwnStartupId = Guid.NewGuid();
    private static readonly Guid OtherStartupId = Guid.NewGuid();

    private static readonly ChangeRequest[] All =
    [
        new() { StartupId = OwnStartupId, PayloadJson = "{}" },
        new() { StartupId = OwnStartupId, PayloadJson = "{}" },
        new() { StartupId = OtherStartupId, PayloadJson = "{}" }
    ];

    [Fact]
    public void SuperAdmin_tum_istekleri_gorur()
    {
        Assert.Equal(3, Apply(FakeCurrentUser.As(UserRole.SuperAdmin)).Length);
    }

    [Fact]
    public void GirisimKullanicisi_yalnizca_kendi_isteklerini_gorur()
    {
        var visible = Apply(new FakeCurrentUser
        {
            Role = UserRole.StartupUser,
            StartupId = OwnStartupId
        });

        Assert.Equal(2, visible.Length);
        Assert.All(visible, c => Assert.Equal(OwnStartupId, c.StartupId));
    }

    [Fact]
    public void GirisimKullanicisi_girisimi_yoksa_hicbir_sey_gormez()
    {
        Assert.Empty(Apply(new FakeCurrentUser { Role = UserRole.StartupUser, StartupId = null }));
    }

    [Fact]
    public void KararVerici_onay_akisinin_tarafi_degil()
    {
        Assert.Empty(Apply(FakeCurrentUser.As(UserRole.DecisionMaker)));
    }

    [Fact]
    public void Kimlik_dogrulanmamis_istek_hicbir_sey_gormez()
    {
        Assert.Empty(Apply(FakeCurrentUser.Anonymous));
    }

    /// <summary>
    /// Öneri gönderme yetkisi role bağlı ve yalnızca girişim kullanıcısında:
    /// diğer roller doğrudan yazar, aynı işi ikinci bir yoldan yapmaları
    /// "onaysız veri yayına girmiyor" kuralını bulanıklaştırırdı.
    /// </summary>
    [Theory]
    [InlineData(UserRole.SuperAdmin, false)]
    [InlineData(UserRole.ProgramManager, false)]
    [InlineData(UserRole.StartupUser, true)]
    [InlineData(UserRole.DecisionMaker, false)]
    public void Oneri_gonderme_yetkisi(UserRole role, bool expected)
    {
        Assert.Equal(expected, Scope(FakeCurrentUser.As(role)).CanSubmit);
    }

    [Theory]
    [InlineData(UserRole.SuperAdmin, true)]
    [InlineData(UserRole.ProgramManager, true)]
    [InlineData(UserRole.StartupUser, false)]
    [InlineData(UserRole.DecisionMaker, false)]
    public void Karar_verme_yetkisi(UserRole role, bool expected)
    {
        Assert.Equal(expected, Scope(FakeCurrentUser.As(role)).CanReview);
    }

    /// <summary>
    /// Program Yöneticisi dışındaki hiçbir rol kararı için sorgu çalıştırmaz;
    /// erişilemez DbContext bunu kanıtlıyor. Program Yöneticisi kapsamı
    /// IStartupScope'a devredildiği için StartupScopeTests ve uçtan uca
    /// senaryoda doğrulanıyor.
    /// </summary>
    private static ChangeRequestScope Scope(FakeCurrentUser user) =>
        new(user, new UnreachableDbContext(), new StartupScope(user));

    private static ChangeRequest[] Apply(FakeCurrentUser user) =>
        Scope(user).Apply(All.AsQueryable()).ToArray();
}
