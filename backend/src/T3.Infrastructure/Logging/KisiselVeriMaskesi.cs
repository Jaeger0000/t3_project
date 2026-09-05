using System.Collections;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;
using T3.Application.Common.Text;

namespace T3.Infrastructure.Logging;

/// <summary>
/// Log yeni bir kişisel veri deposudur. Bir nesne <c>{@Değişken}</c> ile
/// (destructure) loglandığında, ad taşıyan hassas alanları ham değerle değil
/// maskeli değerle yazar. Ürünün her ekranı role göre maskelenirken
/// <c>LogInformation("{@Startup}", startup)</c> satırının ham vergi numarasını
/// rol kapısı olmayan bir dosyaya/Loki'ye yazması KVKK deliği açardı.
///
/// Alan adı listesi tek yerde durur: yeni hassas alan eklendiğinde buraya da
/// eklenir; <c>rbac-kvkk-denetcisi</c> ajanının kontrol listesine bu dosya girer.
/// </summary>
public sealed class KisiselVeriMaskesi : IDestructuringPolicy
{
    private static readonly HashSet<string> HassasAlanlar = new(StringComparer.OrdinalIgnoreCase)
    {
        "Email", "ContactEmail", "Phone", "ContactPhone", "TaxNumber", "Amount"
    };

    public bool TryDestructure(
        object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        var type = value.GetType();

        // Yalnızca düz CLR nesneleri (entity/DTO) hedeflenir; ilkel tipler,
        // string ve koleksiyonlar kendi varsayılan destructuring yoluna
        // bırakılır — aksi hâlde her liste elemanı burada yeniden işlenir.
        if (type.IsPrimitive || type.IsEnum || value is string || value is IEnumerable)
        {
            result = null!;
            return false;
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        if (!properties.Any(p => HassasAlanlar.Contains(p.Name)))
        {
            // Hassas alan taşımayan nesnede devre dışı kal: Serilog kendi
            // varsayılan destructuring'iyle devam eder.
            result = null!;
            return false;
        }

        var structureProperties = properties
            .Where(p => p.GetIndexParameters().Length == 0)
            .Select(p =>
            {
                var rawValue = SafeRead(p, value);
                var loggedValue = HassasAlanlar.Contains(p.Name) ? Mask(p.Name, rawValue) : rawValue;

                return new LogEventProperty(
                    p.Name, propertyValueFactory.CreatePropertyValue(loggedValue, destructureObjects: true));
            })
            .ToList();

        result = new StructureValue(structureProperties, type.Name);
        return true;
    }

    private static object? SafeRead(PropertyInfo property, object value)
    {
        try
        {
            return property.GetValue(value);
        }
        catch
        {
            // Bir alanın okunması (ör. lazy-load proxy) patlarsa tüm log
            // satırını düşürmek yerine "okunamadı" yazılır.
            return "(okunamadı)";
        }
    }

    private static object? Mask(string propertyName, object? rawValue)
    {
        if (rawValue is null)
            return null;

        return propertyName switch
        {
            "Email" or "ContactEmail" => MaskedEmail.Of(rawValue as string),
            "Phone" or "ContactPhone" or "TaxNumber" or "Amount" => "***",
            _ => rawValue
        };
    }
}
