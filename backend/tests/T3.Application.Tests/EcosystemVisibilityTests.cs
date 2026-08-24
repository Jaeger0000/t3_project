using T3.Application.Common.Rbac;
using T3.Domain.Identity;

namespace T3.Application.Tests;

/// <summary>
/// Agregat maskeleme kuralı (Faz 5). Brief finansal veriyi Karar Verici'ye
/// "yalnızca agregat" düzeyinde açıyor; bu testler o ince ayrımı kilitliyor —
/// satırda kapalı, toplamda açık.
/// </summary>
public class EcosystemVisibilityTests
{
    private static readonly Guid Startup = Guid.NewGuid();

    [Fact]
    public void Karar_verici_toplami_gorur_satiri_gormez()
    {
        var user = FakeCurrentUser.As(UserRole.DecisionMaker);

        Assert.True(StartupVisibility.Aggregate(user).ShowExactAmounts);
        Assert.False(StartupVisibility.For(user, Startup).ShowExactAmounts);
    }

    [Fact]
    public void Karar_vericinin_kisisel_veri_kapisi_agregatta_da_kapali()
    {
        // Tutar açılıyor diye iletişim/vergi/doküman da açılmamalı: agregatın
        // gerekçesi "kişi belirtmiyor", bu gerekçe kişisel alanlar için geçmez.
        var aggregate = StartupVisibility.Aggregate(FakeCurrentUser.As(UserRole.DecisionMaker));

        Assert.False(aggregate.ShowContactDetails);
        Assert.False(aggregate.ShowTaxNumber);
        Assert.False(aggregate.ShowTeamPersonalData);
        Assert.False(aggregate.ShowDocuments);
    }

    [Fact]
    public void Super_yonetici_agregatta_da_her_seyi_gorur()
    {
        Assert.Equal(
            StartupVisibility.All,
            StartupVisibility.Aggregate(FakeCurrentUser.As(UserRole.SuperAdmin)));
    }

    [Fact]
    public void Program_yoneticisi_agregatta_vergi_numarasini_goremez()
    {
        var aggregate = StartupVisibility.Aggregate(FakeCurrentUser.As(UserRole.ProgramManager));

        Assert.True(aggregate.ShowExactAmounts);
        Assert.False(aggregate.ShowTaxNumber);
    }

    [Fact]
    public void Girisim_kullanicisi_kendi_karnesinin_tamamini_gorur()
    {
        // Kapsamı zaten tek girişim; agregat da kendi verisi.
        var user = new FakeCurrentUser { Role = UserRole.StartupUser, StartupId = Startup };

        Assert.Equal(StartupVisibility.All, StartupVisibility.Aggregate(user));
    }

    [Fact]
    public void Kimliksiz_istek_hicbir_seyi_gormez()
    {
        Assert.Equal(
            StartupVisibility.None,
            StartupVisibility.Aggregate(FakeCurrentUser.Anonymous));
    }
}
