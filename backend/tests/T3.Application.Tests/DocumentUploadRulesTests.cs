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

    /// <summary>
    /// Uzantı yalnız yeterli değil: ".pdf" adlı bir HTML dosyası indirmede
    /// tarayıcıda çalıştırılabilir bir içerik taşırdı (bkz. G-08).
    /// </summary>
    [Fact]
    public async Task Icerik_uzantiyla_uyusmuyorsa_reddedilir()
    {
        await using var sahteHtml = new MemoryStream("<script>alert(1)</script>"u8.ToArray());

        var matches = await DocumentUploadRules.ContentMatchesExtensionAsync(sahteHtml, ".pdf", default);

        Assert.False(matches);
    }

    [Fact]
    public async Task Gercek_pdf_imzasi_kabul_edilir()
    {
        await using var gercekPdf = new MemoryStream("%PDF-1.7 ..."u8.ToArray());

        var matches = await DocumentUploadRules.ContentMatchesExtensionAsync(gercekPdf, ".pdf", default);

        Assert.True(matches);
    }

    /// <summary>Serbest metin türlerinin imzası yok; her içerik geçer.</summary>
    [Fact]
    public async Task Imzasi_olmayan_uzanti_her_zaman_gecer()
    {
        await using var herhangi = new MemoryStream("ne olursa olsun"u8.ToArray());

        Assert.True(await DocumentUploadRules.ContentMatchesExtensionAsync(herhangi, ".csv", default));
        Assert.True(await DocumentUploadRules.ContentMatchesExtensionAsync(herhangi, ".txt", default));
    }

    [Fact]
    public async Task Kontrolden_sonra_akis_basa_sarilir()
    {
        await using var gercekPng = new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3]);

        await DocumentUploadRules.ContentMatchesExtensionAsync(gercekPng, ".png", default);

        Assert.Equal(0, gercekPng.Position);
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
