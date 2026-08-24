using T3.Application.Common.Text;

namespace T3.Application.Tests;

/// <summary>
/// Başarısız giriş denemeleri denetim izine düşüyor; iz, denenen adreslerin
/// ham listesine dönüşmemeli. Maskeleme alan adını koruyup yerel kısmı
/// düşürüyor: güvenlik incelemesinin sorusu "hangi kurumdan deniyorlar".
/// </summary>
public class MaskedEmailTests
{
    [Fact]
    public void Yerel_kisimdan_yalnizca_ilk_harf_kalir()
    {
        Assert.Equal("k***@t3ekosistem.test",
            MaskedEmail.Of("kulucka.yoneticisi@t3ekosistem.test"));
    }

    [Fact]
    public void Bosluklar_kirpilir()
    {
        Assert.Equal("a***@t3ekosistem.test", MaskedEmail.Of("  admin@t3ekosistem.test "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_deger_kayit_edilebilir_bir_etiket_uretir(string? value)
    {
        Assert.Equal("(boş)", MaskedEmail.Of(value));
    }

    [Theory]
    [InlineData("@t3ekosistem.test")]
    [InlineData("adres-degil")]
    public void Bicimsiz_deger_hicbir_sey_sizdirmaz(string value)
    {
        // Doğrulayıcı bu gövdeyi zaten reddediyor; yine de ize düşen değer
        // kullanıcının yazdığı metni taşımamalı.
        Assert.Equal("***", MaskedEmail.Of(value));
    }
}
