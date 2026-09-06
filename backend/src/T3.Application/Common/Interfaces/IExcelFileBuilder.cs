namespace T3.Application.Common.Interfaces;

/// <summary>
/// Excel (.xlsx) üretimi sağlayıcıdan bağımsız bu arayüz üzerinden çağrılır;
/// gerçek kütüphane yalnızca Infrastructure'da bilinir — <see cref="IReportPdfRenderer"/>
/// ile aynı desen. Hücreler bilinçli olarak metin: CSV/Excel dışa aktarmasıyla
/// aynı maskeleme ve biçimlendirme mantığını (bkz. CsvBuilder) iki yerde ayrı
/// ayrı tutmamak için.
/// </summary>
public interface IExcelFileBuilder
{
    byte[] Build(
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string?>> rows);
}
