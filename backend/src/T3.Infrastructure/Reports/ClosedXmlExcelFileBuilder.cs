using ClosedXML.Excel;
using T3.Application.Common.Interfaces;

namespace T3.Infrastructure.Reports;

/// <summary>
/// <see cref="IExcelFileBuilder"/>'ın ClosedXML uygulaması. Hücreler her zaman
/// metin olarak yazılır (<c>SetValue</c> yerine <c>.Value = string</c> ile
/// dizgi tipi zorlanır): "=" ile başlayan bir başlık ya da hücre, CSV'nin
/// aksine burada formül olarak yorumlanmaz, çünkü OOXML hücre tipini XML'de
/// açıkça "metin" olarak işaretler — CSV'de olduğu gibi Excel'in içeriğe
/// bakıp tahmin etmesi söz konusu değil.
/// </summary>
public sealed class ClosedXmlExcelFileBuilder : IExcelFileBuilder
{
    private static readonly char[] InvalidSheetNameChars = ['\\', '/', '?', '*', '[', ']', ':'];

    public byte[] Build(
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string?>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SanitizeSheetName(sheetName));

        for (var col = 0; col < headers.Count; col++)
        {
            var cell = sheet.Cell(1, col + 1);
            cell.SetValue(headers[col]);
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F4");
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var col = 0; col < row.Count; col++)
                sheet.Cell(rowIndex + 2, col + 1).SetValue(row[col] ?? string.Empty);
        }

        if (headers.Count > 0)
            sheet.SheetView.FreezeRows(1);

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>Excel sayfa adı en çok 31 karakter olur ve bazı karakterleri kabul etmez.</summary>
    private static string SanitizeSheetName(string name)
    {
        var cleaned = new string(name.Select(c => InvalidSheetNameChars.Contains(c) ? '-' : c).ToArray());
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }
}
