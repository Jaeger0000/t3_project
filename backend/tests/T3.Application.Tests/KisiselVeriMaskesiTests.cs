using Serilog;
using Serilog.Events;
using T3.Infrastructure.Logging;
using Xunit;

namespace T3.Application.Tests;

/// <summary>
/// Loglama planının Faz C'si: bir nesne <c>{@...}</c> ile loglandığında hassas
/// alanların ham değerle çıktıya girmediğini doğrular. Bu test, maskeleme
/// kuralının sessizce gevşemesini engelleyen tek mekanizmadır.
/// </summary>
public class KisiselVeriMaskesiTests
{
    private sealed record OrnekKisi(string Ad, string Email, string Phone, decimal Amount);

    [Fact]
    public void HassasAlanlar_HamDegerle_Loglanmaz()
    {
        var kisi = new OrnekKisi("Ayşe Yılmaz", "ayse.yilmaz@example.test", "5551234567", 125000m);

        var yakalanan = new List<LogEvent>();
        using var logger = new LoggerConfiguration()
            .Destructure.With<KisiselVeriMaskesi>()
            .WriteTo.Sink(new YakalayanSink(yakalanan))
            .CreateLogger();

        logger.Information("Kayıt: {@Kisi}", kisi);

        var satir = Render(yakalanan.Single());

        Assert.DoesNotContain(kisi.Email, satir);
        Assert.DoesNotContain(kisi.Phone, satir);
        Assert.DoesNotContain("125000", satir);
        Assert.Contains("Ayşe Yılmaz", satir); // hassas olmayan alan ham kalır
        Assert.Contains("a***@example.test", satir); // MaskedEmail.Of ile aynı kural
    }

    private static string Render(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        logEvent.RenderMessage(writer);
        return writer.ToString();
    }

    private sealed class YakalayanSink(List<LogEvent> hedef) : Serilog.Core.ILogEventSink
    {
        public void Emit(LogEvent logEvent) => hedef.Add(logEvent);
    }
}
