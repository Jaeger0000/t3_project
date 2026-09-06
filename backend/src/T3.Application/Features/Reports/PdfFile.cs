namespace T3.Application.Features.Reports;

/// <summary>İndirilebilir PDF. <see cref="CsvFile"/> ile aynı şekli taşır,
/// ayrı tip olarak duruyor çünkü içerik türü sabit ve dosya adı üretimi
/// farklı (bkz. GenerateStartupReportHandler).</summary>
public sealed record PdfFile(string FileName, string ContentType, byte[] Content);
