using T3.Application.Common.Results;
using T3.Application.Features.Documents;

namespace T3.Application.Tests;

/// <summary>
/// Yükleme kuralları güvenlik sınırının parçası: kullanıcı dosyası hem depoya
/// hem indirme yanıtına giriyor. Bu testler üç sözü kilitliyor — tür beyaz
/// listeyle sınırlı, içerik tipi istemciden alınmıyor, dosya adı yol taşımıyor.
/// </summary>
public class DocumentUploadRulesTests
{
    private static Result<string> Check(string? fileName, long size = 1024) =>
        DocumentUploadRules.Check(fileName, size);

    [Fact]
    public void Izin_verilen_uzanti_kabul_edilir()
    {
        var result = Check("2025_yatirimci_sunumu.pdf");

        Assert.True(result.IsSuccess);
        Assert.Equal("application/pdf", result.Value);
    }

    /// <summary>
    /// İçerik tipi uzantıdan türetiliyor, istemciden alınmıyor: ".pdf" adlı bir
    /// dosyayı "text/html" olarak işaretleyip indirme anında tarayıcıda
    /// çalıştırmayı denemenin önü kapalı.
    /// </summary>
    [Fact]
    public void Icerik_tipi_uzantidan_turetilir()
    {
        Assert.Equal("image/png", Check("logo.PNG").Value);
        Assert.Equal("text/csv", Check("ciro.csv").Value);
    }

    [Theory]
    [InlineData("zararli.exe")]
    [InlineData("betik.sh")]
    [InlineData("sayfa.html")]
    [InlineData("uzantisiz")]
    public void Beyaz_listede_olmayan_tur_reddedilir(string fileName)
    {
        var result = Check(fileName);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void Boyut_siniri_uygulanir()
    {
        Assert.False(Check("rapor.pdf", DocumentUploadRules.MaxSizeBytes + 1).IsSuccess);
        Assert.True(Check("rapor.pdf", DocumentUploadRules.MaxSizeBytes).IsSuccess);
        Assert.False(Check("rapor.pdf", 0).IsSuccess);
    }

    /// <summary>
    /// Tarayıcı bazı durumlarda tam yol gönderir; ad olduğu gibi saklanırsa
    /// indirme başlığına yol bileşeni sızar.
    /// </summary>
    [Theory]
    [InlineData("../../etc/passwd.pdf", "passwd.pdf")]
    [InlineData("C:\\Users\\demo\\rapor.pdf", "rapor.pdf")]
    [InlineData("  bosluklu.pdf  ", "bosluklu.pdf")]
    public void Dosya_adindan_yol_bilesenleri_temizlenir(string input, string expected)
    {
        Assert.Equal(expected, DocumentUploadRules.SafeFileName(input));
    }

    /// <summary>
    /// Satır sonu ve tırnak Content-Disposition başlığına enjeksiyon yolu.
    /// </summary>
    [Fact]
    public void Dosya_adinda_kontrol_karakteri_ve_tirnak_kalmaz()
    {
        var cleaned = DocumentUploadRules.SafeFileName("rapor\r\n\"kotu\".pdf");

        Assert.Equal("raporkotu.pdf", cleaned);
    }

    [Fact]
    public void Bos_dosya_adi_reddedilir()
    {
        Assert.Null(DocumentUploadRules.SafeFileName("   "));
        Assert.False(Check(null).IsSuccess);
    }

    [Fact]
    public void Boyut_etiketi_okunur_bicimde_uretilir()
    {
        Assert.Equal("512 B", DocumentLabels.Size(512));
        // Kültür sunucunun yerel ayarına bırakılmıyor; ondalık ayırıcı her
        // makinede virgül.
        Assert.Equal("1,5 KB", DocumentLabels.Size(1536));
        Assert.Equal("2 MB", DocumentLabels.Size(2 * 1024 * 1024));
    }
}
