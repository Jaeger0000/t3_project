using T3.Infrastructure.Notifications;

namespace T3.Application.Tests;

/// <summary>
/// Gerçek SMTP'ye bağlanmadan doğrulanabilecek iki şey var ve ikisi de
/// canlıda sessizce yanlış gidebilecek türden: gönderici seçiminin
/// yapılandırmaya bakması, ve mesaj başlıklarının (gönderen, yanıt adresi,
/// düz metin gövde) doğru kurulması. Bağlantı kurmayı test etmiyoruz —
/// onu ancak sağlayıcı söyler.
/// </summary>
public class EpostaGonderimTests
{
    private static EmailOptions Ayarlar() => new()
    {
        From = "no-reply@t3girisimportali.test",
        FromName = "T3 Girişim Ekosistemi",
        Smtp =
        {
            Host = "smtp-relay.ornek.test",
            User = "kullanici@ornek.test",
            Password = "smtp-anahtari"
        }
    };

    [Fact]
    public void Smtp_bilgileri_tamsa_yapilandirilmis_sayilir()
    {
        Assert.True(Ayarlar().Smtp.IsConfigured);
    }

    [Theory]
    [InlineData("", "kullanici@ornek.test", "parola")]
    [InlineData("smtp.ornek.test", "", "parola")]
    [InlineData("smtp.ornek.test", "kullanici@ornek.test", "")]
    [InlineData("smtp.ornek.test", "kullanici@ornek.test", "   ")]
    public void Yarim_yapilandirma_gercek_gonderici_secmez(string host, string user, string password)
    {
        var smtp = new SmtpOptions { Host = host, User = user, Password = password };

        // Yarım yapılandırma kabul edilseydi hata ancak ilk şifre sıfırlama
        // isteğinde, üretimde ortaya çıkardı.
        Assert.False(smtp.IsConfigured);
    }

    [Fact]
    public void Gonderen_adi_ve_adresi_mesaja_islenir()
    {
        var message = SmtpEmailSender.BuildMessage(
            Ayarlar(), "ayse@ornek.test", "konu", "gövde");

        var gonderen = Assert.Single(message.From.Mailboxes);
        Assert.Equal("no-reply@t3girisimportali.test", gonderen.Address);
        Assert.Equal("T3 Girişim Ekosistemi", gonderen.Name);

        var alici = Assert.Single(message.To.Mailboxes);
        Assert.Equal("ayse@ornek.test", alici.Address);
        Assert.Equal("konu", message.Subject);
    }

    [Fact]
    public void Yanit_adresi_tanimliysa_baslik_eklenir()
    {
        var options = Ayarlar();
        options.ReplyTo = "info@t3girisimportali.test";

        var message = SmtpEmailSender.BuildMessage(options, "ayse@ornek.test", "konu", "gövde");

        // "no-reply" kutusu açılmıyor; yanıtın okunan kutuya gitmesi gerekiyor.
        var yanit = Assert.Single(message.ReplyTo.Mailboxes);
        Assert.Equal("info@t3girisimportali.test", yanit.Address);
    }

    [Fact]
    public void Yanit_adresi_bos_birakilirsa_baslik_hic_eklenmez()
    {
        var options = Ayarlar();
        options.ReplyTo = "   ";

        var message = SmtpEmailSender.BuildMessage(options, "ayse@ornek.test", "konu", "gövde");

        // Boş bir Reply-To başlığı bazı sağlayıcılarda mesajı reddettiriyor.
        Assert.Empty(message.ReplyTo.Mailboxes);
    }

    [Fact]
    public void Govde_duz_metin_gider_ve_baglanti_bozulmaz()
    {
        const string link = "https://t3girisimportali.test/sifre-sifirla/abc-123_XYZ";

        var message = SmtpEmailSender.BuildMessage(
            Ayarlar(), "ayse@ornek.test", "konu", $"Bağlantı:\n\n{link}\n");

        // HTML gövde yok: sıfırlama bağlantısı kullanıcıya olduğu gibi
        // görünmeli, bir bağlantı metninin arkasına gizlenmemeli.
        Assert.Null(message.HtmlBody);
        Assert.Contains(link, message.TextBody);
    }
}
