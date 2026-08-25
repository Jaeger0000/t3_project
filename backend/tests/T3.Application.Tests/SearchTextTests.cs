using T3.Application.Common.Text;

namespace T3.Application.Tests;

/// <summary>
/// Türkçe küçültme farkının regresyon testleri.
///
/// Bu testler somut bir hatadan doğdu: <c>"İstanbul".ToLowerInvariant()</c>
/// noktalı büyük İ'yi hiç değiştirmiyor, PostgreSQL'in <c>lower()</c> işlevi
/// ise onu düz <c>i</c> yapıyor. İki taraf eşleşmediği için şehir filtresi hiç
/// sonuç vermiyordu. Aşağıdaki beklentiler PostgreSQL'in davranışını sabitler.
/// </summary>
public class SearchTextTests
{
    [Fact]
    public void Noktali_buyuk_I_tek_kod_noktasina_iner()
    {
        var normalized = SearchText.Normalize("İstanbul");

        Assert.Equal("istanbul", normalized);
        Assert.Equal(8, normalized.Length);
    }

    [Fact]
    public void Duz_ToLowerInvariant_bu_isi_yapmaz()
    {
        var wrong = "İstanbul".ToLowerInvariant();

        // Hatanın kaynağını belgeleyen test: invariant küçültme U+0130'a
        // dokunmaz, harf olduğu gibi kalır. PostgreSQL ise onu 'i' yapar.
        Assert.NotEqual("istanbul", wrong);
        Assert.Equal('İ', wrong[0]);
        Assert.Equal(0x0130, wrong[0]);
    }

    [Theory]
    [InlineData("  Ankara  ", "ankara")]
    [InlineData("IZMIR", "izmir")]
    // Noktasız ı ve ş korunur; yalnızca ASCII I küçültülür.
    [InlineData("Işık Teknoloji", "işık teknoloji")]
    [InlineData("ÇANKAYA", "çankaya")]
    [InlineData("Şişli", "şişli")]
    [InlineData("admin@T3Ekosistem.test", "admin@t3ekosistem.test")]
    public void Bilinen_girdiler_beklendigi_gibi_normalize_edilir(string input, string expected)
    {
        Assert.Equal(expected, SearchText.Normalize(input));
    }

    [Theory]
    // Aksansız klavyeyle aranan terim aksanlı kaydı bulmalı.
    [InlineData("Sağlık", "saglik")]
    [InlineData("saglik", "saglik")]
    [InlineData("Şanlıurfa", "sanliurfa")]
    [InlineData("ÇAĞRI ÖZÜ", "cagri ozu")]
    [InlineData("İstanbul", "istanbul")]
    public void Fold_aksanlari_ASCII_karsiligina_indirir(string input, string expected) =>
        Assert.Equal(expected, SearchText.Fold(input));

    [Fact]
    public void Normalize_katlama_yapmaz()
    {
        // Ayrım bilinçli: Normalize'ın çıktısı veritabanındaki katlanmamış
        // kolonla karşılaştırılıyor (e-posta eşitliği, isim tekilliği).
        // Katlamayı oraya taşımak girişi sessizce bozardı.
        Assert.Equal("sağlık", SearchText.Normalize("Sağlık"));
        Assert.Equal("saglik", SearchText.Fold("Sağlık"));
    }
}
