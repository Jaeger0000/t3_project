using System.Reflection;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using T3.Application.Common.Interfaces;

namespace T3.Infrastructure.Reports;

/// <summary>
/// AI raporunun PDF şablonu. Sabit kodlu — model yalnızca
/// <see cref="ReportSectionContent.Body"/> metnini doldurur, sayfa düzeni,
/// marka ve tipografi burada tanımlı.
///
/// Model çıktısı yalnızca dar bir biçim alt kümesi kullanıyor (bkz.
/// <c>ReportSections.FormatInstruction</c>): düz paragraf, "- " ile başlayan
/// madde imi, "**kalın**" vurgu. <see cref="ParseBody"/> yalnızca bunları
/// ayrıştırıyor — model daha zengin bir biçim (tablo, başlık) denerse düz
/// metin olarak basılır, sayfa bozulmaz.
/// </summary>
public sealed class QuestPdfReportRenderer : IReportPdfRenderer
{
    private const string BrandColor = "#E73A13";
    private const string BrandDark = "#B52205";
    private const string InkColor = "#1C1917";
    private const string MutedColor = "#78716C";

    private static readonly byte[] LogoBytes = LoadLogo();

    static QuestPdfReportRenderer()
    {
        // Community lisansı: küçük/gelir sınırı olmayan (bu proje kapsamında
        // ticari olmayan bir Creathon prototipi) kullanım için ücretsiz.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(StartupReportDocument document) =>
        QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                // Font açıkça veriliyor: sistemin varsayılan yazı tipi (değişken
                // genişlikli, bağlam duyarlı biçimlendirme uygulayan bir yazı
                // tipi) "ti"/"tt" gibi harf çiftlerini görsel olarak doğru
                // basıyor ama PDF'in metin katmanına (kopyala/yapıştır, arama,
                // ekran okuyucu) bu harfleri hiç yazmıyordu — sayfa görünüşte
                // kusursuzken metni kopyalayınca "üretim" "ürem" oluyordu.
                // Liberation Sans böyle bir bitişik harf kullanmıyor.
                page.DefaultTextStyle(x => x.FontFamily("Liberation Sans").FontSize(10.5f).FontColor(InkColor));

                page.Header().Element(c => ComposeHeader(c, document));
                page.Content().Element(c => ComposeContent(c, document));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();

    private static void ComposeHeader(IContainer container, StartupReportDocument document)
    {
        container.PaddingBottom(12).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.ConstantItem(48).Image(LogoBytes).FitArea();

                row.RelativeItem().PaddingLeft(10).Column(inner =>
                {
                    inner.Item().Text("T3 Vakfı Girişim Ekosistemi Yönetim Sistemi")
                        .FontSize(9).FontColor(MutedColor);
                    inner.Item().Text("Girişim AI Raporu")
                        .FontSize(16).Bold().FontColor(InkColor);
                });

                row.ConstantItem(140).AlignRight().Column(inner =>
                {
                    inner.Item().AlignRight().Text(document.GeneratedAt.ToString("dd.MM.yyyy HH:mm"))
                        .FontSize(9).FontColor(MutedColor);
                    inner.Item().AlignRight().Text($"Hazırlayan: {document.GeneratedByName}")
                        .FontSize(8).FontColor(MutedColor);
                    inner.Item().AlignRight().Text(document.GeneratedByRoleLabel)
                        .FontSize(8).FontColor(MutedColor);
                });
            });

            column.Item().PaddingTop(10).Background(BrandColor).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Text(document.StartupName)
                        .FontSize(15).Bold().FontColor(Colors.White);

                    var meta = string.Join("  ·  ", new[]
                    {
                        document.SectorLabel,
                        document.City,
                        document.StatusLabel,
                    }.Where(v => !string.IsNullOrWhiteSpace(v)));

                    if (meta.Length > 0)
                        inner.Item().Text(meta).FontSize(9).FontColor(Colors.White);
                });
            });

            column.Item().PaddingTop(2).LineHorizontal(1).LineColor(BrandColor);
        });
    }

    private static void ComposeContent(IContainer container, StartupReportDocument document)
    {
        container.PaddingTop(8).Column(column =>
        {
            column.Spacing(14);

            foreach (var section in document.Sections)
                column.Item().Element(c => ComposeSection(c, section));
        });
    }

    private static void ComposeSection(IContainer container, ReportSectionContent section)
    {
        container.Column(column =>
        {
            column.Item()
                .BorderBottom(1.5f).BorderColor(BrandColor)
                .PaddingBottom(3)
                .Text(section.Title).FontSize(12.5f).Bold().FontColor(BrandDark);

            column.Item().PaddingTop(6).Element(c => ComposeBody(c, section.Body));
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.PaddingTop(6).Column(column =>
        {
            column.Item().LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);

            column.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(
                    "Bu rapor T3 Vakfı Girişim Ekosistemi Yönetim Sistemi tarafından yapay "
                    + "zekâ desteğiyle otomatik üretilmiştir; görüntüleyenin yetki seviyesine "
                    + "göre maskelenmiş verilere dayanır.").FontSize(7).FontColor(MutedColor);

                row.ConstantItem(90).AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor(MutedColor));
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });
    }

    /// <summary>
    /// Boş satırla ayrılmış paragraflar, "- " ile başlayan madde imleri ve
    /// "**kalın**" vurgu dışında hiçbir biçim beklenmiyor (bkz. sınıf
    /// yorumu). Bilinmeyen bir kalıp düz metin olarak basılır.
    /// </summary>
    private static void ComposeBody(IContainer container, string body)
    {
        var blocks = body.Replace("\r\n", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        container.Column(column =>
        {
            column.Spacing(6);

            foreach (var block in blocks)
            {
                var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var isBulletBlock = lines.Length > 0 && lines.All(l => l.StartsWith("- "));

                if (isBulletBlock)
                {
                    column.Item().Column(list =>
                    {
                        list.Spacing(3);
                        foreach (var line in lines)
                            list.Item().Row(row =>
                            {
                                row.ConstantItem(10).Text("•").FontColor(BrandDark);
                                row.RelativeItem().Text(text => ComposeSpans(text, line[2..]));
                            });
                    });
                }
                else
                {
                    column.Item().Text(text =>
                    {
                        text.ParagraphSpacing(2);
                        ComposeSpans(text, block.Replace('\n', ' '));
                    });
                }
            }
        });
    }

    /// <summary>"**vurgu**" işaretli parçaları kalın, kalanını düz basar.</summary>
    private static void ComposeSpans(TextDescriptor text, string raw)
    {
        var parts = raw.Split("**");

        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;

            var span = text.Span(parts[i]);
            if (i % 2 == 1) span.Bold();
        }
    }

    private static byte[] LoadLogo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var name = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith("tgm-logo-isaret.png", StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(name)!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
