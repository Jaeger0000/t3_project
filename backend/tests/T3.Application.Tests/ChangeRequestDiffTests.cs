using T3.Application.Common.Rbac;
using T3.Application.Features.Achievements;
using T3.Application.Features.Approvals;
using T3.Application.Features.Documents;
using T3.Domain.Achievements;
using T3.Domain.Documents;
using T3.Application.Features.Startups;
using T3.Application.Features.Startups.Team;
using T3.Domain.Startups;

namespace T3.Application.Tests;

/// <summary>
/// Onay ekranının diff üretimi. İki ayrı sözü aynı anda tutmak zorunda:
/// inceleyici hangi alanın değiştiğini görecek, görmeye yetkisi olmadığı
/// değerleri görmeyecek. Bu testler o iki sözü birbirinden bağımsız kilitliyor.
/// </summary>
public class ChangeRequestDiffTests
{
    private static StartupWriteModel Model(
        string name = "Anadolu Robotik",
        string? taxNumber = "1234567890",
        string? contactEmail = "iletisim@ornek.test",
        StartupStatus? status = StartupStatus.Active) =>
        new(name, "Anadolu Robotik A.Ş.", taxNumber, new DateOnly(2021, 5, 4),
            Sector.Defense, ["Robotik"], "Ürün", null, null, "Ankara",
            contactEmail, "+90 555 000 00 00", status);

    private static DiffFieldResponse Field(
        IReadOnlyList<DiffFieldResponse> fields, string name) =>
        fields.Single(f => f.Field == name);

    /// <summary>
    /// Program Yöneticisi vergi numarasını girişim kartında göremiyor; onay
    /// ekranı bu kuralın kaçış yolu olmamalı. Ama kararı verebilmesi için
    /// alanın değiştiğini bilmesi gerekiyor.
    /// </summary>
    [Fact]
    public void Yetkisiz_alanda_degisiklik_gorunur_deger_gorunmez()
    {
        var visibility = StartupVisibility.For(
            FakeCurrentUser.As(Domain.Identity.UserRole.ProgramManager), Guid.NewGuid());

        var fields = ChangeRequestDiff.ForStartup(
            Model(taxNumber: "1234567890"),
            Model(taxNumber: "9876543210"),
            visibility);

        var tax = Field(fields, "taxNumber");

        Assert.True(tax.Changed);
        Assert.True(tax.Masked);
        Assert.Null(tax.Before);
        Assert.Null(tax.After);
    }

    [Fact]
    public void Yetkili_inceleyici_degeri_gorur()
    {
        var fields = ChangeRequestDiff.ForStartup(
            Model(taxNumber: "1234567890"),
            Model(taxNumber: "9876543210"),
            StartupVisibility.All);

        var tax = Field(fields, "taxNumber");

        Assert.True(tax.Changed);
        Assert.False(tax.Masked);
        Assert.Equal("1234567890", tax.Before);
        Assert.Equal("9876543210", tax.After);
    }

    /// <summary>
    /// Maskelenmiş ama değişmemiş alan da doğru raporlanmalı: maskeleme
    /// "değişti" sanılmasına yol açarsa inceleyici olmayan bir değişikliği
    /// onaylar.
    /// </summary>
    [Fact]
    public void Maskeleme_degismemis_alani_degismis_gostermez()
    {
        var fields = ChangeRequestDiff.ForStartup(
            Model(), Model(), StartupVisibility.None);

        var tax = Field(fields, "taxNumber");

        Assert.False(tax.Changed);
        Assert.True(tax.Masked);
    }

    /// <summary>
    /// Durum alanında null "değişmiyor" demek (StartupWriteModel.ApplyTo).
    /// Diff aynı anlamı vermezse formda gönderilmeyen alan "Faal → (boş)"
    /// görünür ve inceleyici olmayan bir değişikliği onaylar.
    /// </summary>
    [Fact]
    public void Gonderilmeyen_durum_alani_degisiklik_sayilmaz()
    {
        var fields = ChangeRequestDiff.ForStartup(
            Model(status: StartupStatus.Active),
            Model(status: null),
            StartupVisibility.All);

        var status = Field(fields, "status");

        Assert.False(status.Changed);
        Assert.Equal("Faal", status.Before);
        Assert.Equal("Faal", status.After);
    }

    [Fact]
    public void Ayni_oneride_hicbir_alan_degismis_gorunmez()
    {
        var fields = ChangeRequestDiff.ForStartup(Model(), Model(), StartupVisibility.All);

        Assert.Equal(0, ChangeRequestDiff.ChangedCount(fields));
    }

    [Fact]
    public void Degisen_alan_sayisi_yalnizca_degisenleri_sayar()
    {
        var fields = ChangeRequestDiff.ForStartup(
            Model(name: "Anadolu Robotik", contactEmail: "eski@ornek.test"),
            Model(name: "Anadolu Robotik Teknoloji", contactEmail: "yeni@ornek.test"),
            StartupVisibility.All);

        Assert.Equal(2, ChangeRequestDiff.ChangedCount(fields));
    }

    /// <summary>Boş metin ile null aynı sayılır: veritabanı ikisini ayırmıyor.</summary>
    [Fact]
    public void Bos_metin_ile_null_ayni_kabul_edilir()
    {
        var fields = ChangeRequestDiff.ForStartup(
            Model() with { LegalName = null },
            Model() with { LegalName = "   " },
            StartupVisibility.All);

        Assert.False(Field(fields, "legalName").Changed);
    }

    [Fact]
    public void Ekip_uyesi_kisisel_verisi_yetkisiz_role_maskelenir()
    {
        var before = new TeamMemberWriteModel(
            "Elif Yıldırım", "CTO", "elif@ornek.test", "+90 555 000 00 01",
            null, true, new DateOnly(2021, 5, 4));

        var after = before with { Email = "elif.yildirim@ornek.test" };

        var fields = ChangeRequestDiff.ForTeamMember(before, after, StartupVisibility.None);
        var email = Field(fields, "email");

        Assert.True(email.Changed);
        Assert.True(email.Masked);
        Assert.Null(email.After);

        // Ad kişisel veri sayılmıyor: karar verilebilmesi için hangi üyeden
        // bahsedildiği bilinmek zorunda.
        Assert.False(Field(fields, "fullName").Masked);
    }

    /// <summary>
    /// Silme önerisinde "sonra" tarafı yok; tüm dolu alanlar değişmiş sayılır,
    /// aksi hâlde kaydın kaldırıldığı ekranda görünmez.
    /// </summary>
    [Fact]
    public void Silme_onerisinde_dolu_alanlar_degisiklik_olarak_gorunur()
    {
        var before = new TeamMemberWriteModel(
            "Elif Yıldırım", "CTO", null, null, null, true, null);

        var fields = ChangeRequestDiff.ForTeamMember(before, null, StartupVisibility.All);

        Assert.True(Field(fields, "fullName").Changed);
        Assert.Null(Field(fields, "fullName").After);
        Assert.False(Field(fields, "email").Changed);
    }

    // --- Başarı kayıtları ve dokümanlar (Faz 4) --------------------------

    private static AchievementWriteModel Revenue(decimal amount, int year = 2024) =>
        new(AchievementKind.Revenue, new DateOnly(year, 12, 31), null,
            amount, "TRY", year, null, null, null, null, null, null,
            null, null, null, null);

    /// <summary>
    /// Tutar yetkisi olmayan bir inceleyici satırı görür, meblağı görmez —
    /// girişim kartındaki kuralın onay ekranındaki karşılığı.
    /// </summary>
    [Fact]
    public void Tutar_yetkisi_yoksa_meblag_diffte_maskelenir()
    {
        var visibility = StartupVisibility.For(
            FakeCurrentUser.As(Domain.Identity.UserRole.DecisionMaker), Guid.NewGuid());

        var fields = ChangeRequestDiff.ForAchievement(
            Revenue(2_800_000m), Revenue(3_400_000m), visibility);

        var amount = Field(fields, "amount");

        Assert.True(amount.Changed);
        Assert.True(amount.Masked);
        Assert.Null(amount.Before);
        Assert.Null(amount.After);
    }

    [Fact]
    public void Yetkili_inceleyici_tutari_gorur()
    {
        var fields = ChangeRequestDiff.ForAchievement(
            Revenue(2_800_000m), Revenue(3_400_000m), StartupVisibility.All);

        var amount = Field(fields, "amount");

        Assert.False(amount.Masked);
        Assert.Equal("2.800.000 TRY", amount.Before);
        Assert.Equal("3.400.000 TRY", amount.After);
    }

    /// <summary>
    /// Tutar metni sunucunun yerel ayarına bırakılmıyor: aynı öneri iki
    /// makinede iki farklı metin üretirse diff yalan söyler.
    /// </summary>
    [Fact]
    public void Tutar_bicimi_kulturden_bagimsizdir()
    {
        var fields = ChangeRequestDiff.ForAchievement(
            null, Revenue(1_234_567.5m), StartupVisibility.All);

        Assert.Equal("1.234.567,5 TRY", Field(fields, "amount").After);
    }

    [Fact]
    public void Silme_onerisinde_sonra_tarafi_bostur()
    {
        var fields = ChangeRequestDiff.ForAchievement(
            Revenue(2_800_000m), null, StartupVisibility.All);

        var amount = Field(fields, "amount");

        Assert.True(amount.Changed);
        Assert.NotNull(amount.Before);
        Assert.Null(amount.After);
    }

    /// <summary>
    /// Doküman diff'i üstveriyle sınırlı; depo yolu bilinçli olarak dışarıda,
    /// iç uygulama ayrıntısının inceleyicinin kararına katkısı yok.
    /// </summary>
    [Fact]
    public void Dokuman_diffi_depo_yolunu_sizdirmaz()
    {
        var proposal = new DocumentProposalModel(
            DocumentType.PitchDeck, "yatirimci_sunumu.pdf",
            "2026/08/9f1c.pdf", "application/pdf", 1536);

        var fields = ChangeRequestDiff.ForDocument(null, proposal, StartupVisibility.All);

        Assert.DoesNotContain(fields, f => f.Field == "storagePath");
        Assert.Equal("yatirimci_sunumu.pdf", Field(fields, "fileName").After);
        Assert.Equal("Sunum", Field(fields, "type").After);
        Assert.Equal("1,5 KB", Field(fields, "sizeBytes").After);
    }

    /// <summary>
    /// Doküman yetkisi olmayan rol dosya adını göremez: ad tek başına ticari
    /// bilgi taşıyabiliyor.
    /// </summary>
    [Fact]
    public void Dokuman_yetkisi_yoksa_dosya_adi_maskelenir()
    {
        var visibility = StartupVisibility.For(
            FakeCurrentUser.As(Domain.Identity.UserRole.DecisionMaker), Guid.NewGuid());

        var proposal = new DocumentProposalModel(
            DocumentType.Contract, "2025_satis_sozlesmesi.pdf",
            "2026/08/9f1c.pdf", "application/pdf", 2048);

        var fields = ChangeRequestDiff.ForDocument(null, proposal, visibility);
        var name = Field(fields, "fileName");

        Assert.True(name.Changed);
        Assert.True(name.Masked);
        Assert.Null(name.After);
    }
}
