using System.Text.Json.Nodes;
using T3.Application.Features.Assistant;

namespace T3.Application.Tests;

/// <summary>
/// AI'a giden araç sonucu REST'in gördüğünden daha sıkı süzülmeli: rolün
/// REST'te görme yetkisi olması, veriyi yurt dışındaki bir model sağlayıcısına
/// aktarmayı kabul ettiği anlamına gelmez (bkz. G-04).
/// </summary>
public class AiRedactionTests
{
    [Fact]
    public void Girisim_iletisim_bilgisi_modele_gitmez()
    {
        const string json = """
            {"name":"Örnek A.Ş.","contactEmail":"info@ornek.test","contactPhone":"05551234567"}
            """;

        var redacted = JsonNode.Parse(AiRedaction.Redact(json))!;

        Assert.Equal("Örnek A.Ş.", redacted["name"]!.GetValue<string>());
        Assert.NotEqual("info@ornek.test", redacted["contactEmail"]!.GetValue<string>());
        Assert.NotEqual("05551234567", redacted["contactPhone"]!.GetValue<string>());
    }

    [Fact]
    public void Vergi_numarasi_modele_gitmez()
    {
        var redacted = AiRedaction.Redact("""{"taxNumber":"1234567890"}""");

        Assert.DoesNotContain("1234567890", redacted);
    }

    [Fact]
    public void Ic_ice_dizideki_ekip_uyesi_kisisel_verisi_temizlenir()
    {
        const string json = """
            {"team":[
                {"fullName":"Ayşe Yılmaz","title":"Kurucu","email":"ayse@ornek.test","linkedInUrl":"https://linkedin.com/in/ayse"}
            ]}
            """;

        var member = JsonNode.Parse(AiRedaction.Redact(json))!["team"]![0]!;

        Assert.NotEqual("Ayşe Yılmaz", member["fullName"]!.GetValue<string>());
        Assert.NotEqual("ayse@ornek.test", member["email"]!.GetValue<string>());
        Assert.NotEqual("https://linkedin.com/in/ayse", member["linkedInUrl"]!.GetValue<string>());

        // Görev unvanı kişisel veri sayılmıyor, kalabilir.
        Assert.Equal("Kurucu", member["title"]!.GetValue<string>());
    }

    [Fact]
    public void Kisisel_veri_tasimayan_alanlar_degismez()
    {
        const string json = """
            {"name":"Örnek A.Ş.","sector":"HealthTech","city":"Ankara","totalCount":3}
            """;

        var redacted = JsonNode.Parse(AiRedaction.Redact(json))!;

        Assert.Equal("Örnek A.Ş.", redacted["name"]!.GetValue<string>());
        Assert.Equal("HealthTech", redacted["sector"]!.GetValue<string>());
        Assert.Equal("Ankara", redacted["city"]!.GetValue<string>());
        Assert.Equal(3, redacted["totalCount"]!.GetValue<int>());
    }
}
