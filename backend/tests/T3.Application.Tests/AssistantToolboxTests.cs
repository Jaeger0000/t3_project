using System.Text.Json;
using T3.Application.Features.Assistant;

namespace T3.Application.Tests;

/// <summary>
/// Araç kataloğunun sözleşmesi. Katalog hem MCP <c>tools/list</c> yanıtını hem
/// modele giden araç listesini besliyor; bozuk bir şema iki yüzeyi birden
/// sessizce kırar.
/// </summary>
public class AssistantToolboxTests
{
    [Fact]
    public void Katalog_bos_degil_ve_adlar_tekil()
    {
        var names = AssistantToolbox.Catalog.Select(t => t.Name).ToArray();

        Assert.NotEmpty(names);
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Plandaki_bes_arac_katalogda_var()
    {
        // Teknik plandaki tool seti; biri düşerse MCP sözleşmesi daralır.
        string[] planned =
        [
            "search_startups", "get_startup_card", "get_program_history",
            "ecosystem_stats", "list_pending_approvals"
        ];

        Assert.All(planned, name =>
            Assert.Contains(AssistantToolbox.Catalog, t => t.Name == name));
    }

    [Fact]
    public void Her_aracin_aciklamasi_ve_nesne_semasi_var()
    {
        foreach (var tool in AssistantToolbox.Catalog)
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.Description));

            var schema = JsonSerializer.SerializeToElement(tool.InputSchema);
            Assert.Equal("object", schema.GetProperty("type").GetString());
            Assert.Equal(JsonValueKind.Object, schema.GetProperty("properties").ValueKind);
        }
    }

    [Fact]
    public void Zorunlu_alanlar_semada_tanimli()
    {
        foreach (var tool in AssistantToolbox.Catalog)
        {
            var schema = JsonSerializer.SerializeToElement(tool.InputSchema);
            var properties = schema.GetProperty("properties");

            foreach (var required in schema.GetProperty("required").EnumerateArray())
            {
                var name = required.GetString()!;
                Assert.True(
                    properties.TryGetProperty(name, out _),
                    $"{tool.Name}: zorunlu '{name}' alanı şemada tanımlı değil.");
            }
        }
    }

    [Fact]
    public void Girisim_kimligi_isteyen_araclarda_zorunlu_isaretli()
    {
        // Model kimliksiz çağırırsa handler zaten hata döner; şemanın bunu
        // baştan söylemesi gereksiz bir tur kazandırır.
        string[] needsId = ["get_startup_card", "get_program_history", "list_achievements"];

        foreach (var name in needsId)
        {
            var tool = AssistantToolbox.Catalog.Single(t => t.Name == name);
            var schema = JsonSerializer.SerializeToElement(tool.InputSchema);

            Assert.Contains(
                schema.GetProperty("required").EnumerateArray().Select(r => r.GetString()),
                value => value == "startupId");
        }
    }
}
