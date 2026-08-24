using System.Globalization;
using System.Text;
using T3.Application.Features.Reports;

namespace T3.Application.Tests;

/// <summary>
/// CSV üretiminin Excel'de bozulmadan açılmasını ve maskelemenin dosyaya da
/// taşınmasını kilitler. Bunlar ekranda değil indirilen dosyada ortaya çıkan,
/// gözden kaçması kolay hatalar.
/// </summary>
public class CsvBuilderTests
{
    [Fact]
    public void Dosya_Utf8_Bom_ile_baslar()
    {
        var bytes = new CsvBuilder("Ad").Row("Şirket").ToBytes();

        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        Assert.Contains("Şirket", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Ayrac_noktali_virgul()
    {
        var text = Text(new CsvBuilder("Ad", "Şehir").Row("Anadolu Robotik", "Ankara"));

        Assert.Equal("Ad;Şehir", text[0]);
        Assert.Equal("Anadolu Robotik;Ankara", text[1]);
    }

    [Fact]
    public void Ayrac_iceren_hucre_tirnaklanir()
    {
        var text = Text(new CsvBuilder("Alanlar").Row("Robotik; Görü"));

        Assert.Equal("\"Robotik; Görü\"", text[1]);
    }

    [Fact]
    public void Tirnak_ikilenir()
    {
        var text = Text(new CsvBuilder("Not").Row("\"acil\" kayıt"));

        Assert.Equal("\"\"\"acil\"\" kayıt\"", text[1]);
    }

    [Fact]
    public void Satir_sonu_hucre_icinde_bosluga_cevrilir()
    {
        var text = Text(new CsvBuilder("Not").Row("birinci\r\nikinci"));

        // Aksi halde tek kayıt iki satıra bölünür ve sütunlar kayar.
        Assert.Equal(2, text.Length);
        Assert.Equal("birinci ikinci", text[1]);
    }

    [Theory]
    [InlineData("=1+1")]
    [InlineData("+90 500 000 00 01")]
    [InlineData("@komut")]
    public void Formul_gibi_baslayan_hucre_kacirilir(string cell)
    {
        var text = Text(new CsvBuilder("Alan").Row(cell));

        Assert.StartsWith("'", text[1].TrimStart('"'));
    }

    [Fact]
    public void Tutar_tr_kulturuyle_bicimlenir()
    {
        // Kültür açıkça veriliyor; makinenin yereline bırakılsa aynı veriden
        // makineye göre değişen dosyalar çıkardı.
        Assert.Equal("1.234.567,5", CsvBuilder.Money(1_234_567.5m, authorized: true));
    }

    [Fact]
    public void Yetkisiz_tutar_bos_degil_aciklama_dondurur()
    {
        // Boş hücre "kayıt yok" demek olurdu; ayrım dosyada da korunmalı.
        Assert.Equal(CsvBuilder.Masked, CsvBuilder.Money(9_000m, authorized: false));
        Assert.Null(CsvBuilder.Money(null, authorized: true));
    }

    [Fact]
    public void Yetkisiz_metin_maskelenir()
    {
        Assert.Equal(CsvBuilder.Masked, CsvBuilder.Text("1234567801", authorized: false));
        Assert.Equal("1234567801", CsvBuilder.Text("1234567801", authorized: true));
    }

    [Fact]
    public void Tarih_gun_ay_yil_olarak_yazilir()
    {
        Assert.Equal("12.04.2022", CsvBuilder.Date(new DateOnly(2022, 4, 12)));
        Assert.Null(CsvBuilder.Date(null));
    }

    [Fact]
    public void Sayi_kultur_bagimsiz_yazilir()
    {
        // Sayım alanları (ekip sayısı, yıl) binlik ayırıcı almamalı: 2025 yılı
        // "2.025" olarak yazılırsa Excel'de metne düşer.
        Assert.Equal("2025", CsvBuilder.Number(2025));
        Assert.Equal("2025", 2025.ToString(CultureInfo.InvariantCulture));
    }

    private static string[] Text(CsvBuilder builder) =>
        Encoding.UTF8.GetString(builder.ToBytes())
            .TrimStart('﻿')
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
}
