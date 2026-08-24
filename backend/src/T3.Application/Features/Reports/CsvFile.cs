using System.Globalization;
using System.Text;

namespace T3.Application.Features.Reports;

/// <summary>İndirilebilir dosya. İçerik bayt dizisi olarak taşınır; uç nokta
/// yalnızca akışa yazar.</summary>
public sealed record CsvFile(string FileName, string ContentType, byte[] Content);

/// <summary>
/// CSV üretimi. İki karar bilinçli:
///
/// 1. <b>Ayraç noktalı virgül.</b> Türkçe yerel ayarda ondalık ayırıcı virgül
///    olduğu için virgülle ayrılmış dosyayı Excel tek sütuna yığıyor.
/// 2. <b>UTF-8 BOM.</b> Excel BOM'suz UTF-8'i yerel kod sayfası sanıp Türkçe
///    karakterleri bozuyor.
///
/// Kültür her çağrıda açıkça veriliyor: makinenin yerel ayarına bırakılan
/// biçimlendirme, aynı veriden farklı dosyalar üretir.
/// </summary>
public sealed class CsvBuilder
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private const char Separator = ';';

    private readonly StringBuilder _buffer = new();

    public CsvBuilder(params string[] headers) => Row(headers);

    public CsvBuilder Row(params string?[] cells)
    {
        _buffer.AppendJoin(Separator, cells.Select(Escape));
        _buffer.Append("\r\n");
        return this;
    }

    /// <summary>
    /// Maskelenmiş hücre. Boş bırakmak yerine açık bir metin yazılıyor: dışa
    /// aktarılan dosyayı okuyan kişi "veri yok" ile "yetki yok" ayrımını
    /// ekranda olduğu gibi burada da görmeli.
    /// </summary>
    public const string Masked = "yetkiniz yok";

    public static string? Money(decimal? amount, bool authorized) =>
        !authorized ? Masked
        : amount is not { } value ? null
        : value.ToString("#,##0.##", Turkish);

    public static string? Number(int? value) =>
        value?.ToString(CultureInfo.InvariantCulture);

    public static string? Date(DateOnly? value) =>
        value?.ToString("dd.MM.yyyy", Turkish);

    public static string? Text(string? value, bool authorized) =>
        authorized ? value : Masked;

    public byte[] ToBytes() => Encoding.UTF8.GetPreamble()
        .Concat(Encoding.UTF8.GetBytes(_buffer.ToString()))
        .ToArray();

    /// <summary>
    /// Hücre kaçırma. Formül enjeksiyonuna karşı da koruyor: '=' ile başlayan
    /// bir hücreyi Excel formül sanıp çalıştırabiliyor, bu yüzden öne tek tırnak
    /// ekleniyor.
    /// </summary>
    private static string Escape(string? cell)
    {
        if (string.IsNullOrEmpty(cell))
            return string.Empty;

        var value = cell.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');

        if (value.Length > 0 && "=+-@\t".Contains(value[0]))
            value = "'" + value;

        return value.Contains(Separator) || value.Contains('"')
            ? '"' + value.Replace("\"", "\"\"") + '"'
            : value;
    }
}
