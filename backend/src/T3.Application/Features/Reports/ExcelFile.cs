namespace T3.Application.Features.Reports;

/// <summary>İndirilebilir Excel dosyası. <see cref="CsvFile"/> ile aynı şekli
/// taşır, ayrı tip olarak duruyor çünkü içerik türü ve dosya uzantısı sabit
/// (bkz. <see cref="PdfFile"/> için aynı gerekçe).</summary>
public sealed record ExcelFile(string FileName, string ContentType, byte[] Content);
