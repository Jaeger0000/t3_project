using T3.Application.Features.Programs;
using T3.Application.Features.Programs.Terms;
using T3.Domain.Identity;
using T3.Domain.Programs;

namespace T3.Application.Tests;

/// <summary>
/// Program yetkisinin iki ayrı kapısı. Program <em>tanımı</em> yalnızca sistem
/// yöneticisinde: program listesi aynı zamanda Program Yöneticisi'nin yetki
/// kapsamının tanımı, kendi kapsamını büyütebilen rol RBAC'ı anlamsız kılar.
/// Rol kararı veritabanına gitmiyor — <see cref="UnreachableDbContext"/> bunu
/// kanıtlanabilir kılıyor.
/// </summary>
public class ProgramAccessRulesTests
{
    private static ProgramAccessGuard Guard(UserRole role) =>
        new(new UnreachableDbContext(), FakeCurrentUser.As(role));

    [Fact]
    public void Program_tanimini_yalnizca_sistem_yoneticisi_yonetir()
    {
        Assert.Null(Guard(UserRole.SuperAdmin).EnsureCanManagePrograms());
    }

    [Theory]
    [InlineData(UserRole.ProgramManager)]
    [InlineData(UserRole.StartupUser)]
    [InlineData(UserRole.DecisionMaker)]
    public void Diger_roller_program_tanimlayamaz(UserRole role)
    {
        var denied = Guard(role).EnsureCanManagePrograms();

        Assert.NotNull(denied);
        Assert.Contains("sistem yöneticisi", denied!.Message);
    }

    [Theory]
    [InlineData(UserRole.StartupUser)]
    [InlineData(UserRole.DecisionMaker)]
    public async Task Donem_yonetimi_de_role_kapali(UserRole role)
    {
        // Rol elverişsizse kapsam sorgusu hiç çalışmamalı: karar rolde biter.
        var denied = await Guard(role).EnsureOwnsProgramAsync(Guid.NewGuid(), default);

        Assert.NotNull(denied);
    }
}

/// <summary>
/// Dönem gövdesinin doğrulaması. Tarih sırası burada tutulmazsa "2026 Bahar"
/// dönemi kendinden önce bitebilir ve gelişim yolculuğu ters sıralanır.
/// </summary>
public class ProgramTermWriteModelTests
{
    private static readonly ProgramTermWriteModelValidator TermValidator = new();
    private static readonly ProgramWriteModelValidator ProgramValidator = new();

    [Fact]
    public void Bitis_baslangictan_once_olamaz()
    {
        var result = TermValidator.Validate(new ProgramTermWriteModel(
            "2026 Bahar", new DateOnly(2026, 3, 1), new DateOnly(2026, 2, 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("başlangıçtan önce"));
    }

    [Fact]
    public void Bitisi_bos_donem_gecerli()
    {
        var result = TermValidator.Validate(new ProgramTermWriteModel(
            "2026 Bahar", new DateOnly(2026, 3, 1), null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Donem_adi_zorunlu()
    {
        var result = TermValidator.Validate(new ProgramTermWriteModel(
            "  ", new DateOnly(2026, 3, 1), null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Donem_adi_kirpilarak_yaziliyor()
    {
        var term = new ProgramTerm();
        new ProgramTermWriteModel("  2026 Bahar  ", new DateOnly(2026, 3, 1), null)
            .ApplyTo(term);

        Assert.Equal("2026 Bahar", term.Name);
    }

    [Fact]
    public void Program_adi_zorunlu_tur_gecerli_olmali()
    {
        var result = ProgramValidator.Validate(new ProgramWriteModel(
            "", (ProgramType)99, null, null));

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Bos_metinler_null_a_ceviriliyor()
    {
        var program = new EcosystemProgram();
        new ProgramWriteModel("Take Off", ProgramType.Acceleration, "   ", "")
            .ApplyTo(program);

        Assert.Null(program.Coordinatorship);
        Assert.Null(program.Description);
    }
}
