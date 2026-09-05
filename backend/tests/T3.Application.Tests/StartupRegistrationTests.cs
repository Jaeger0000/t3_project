using T3.Application.Common.Interfaces;
using T3.Application.Features.Auth.Register;
using T3.Application.Features.Registrations.ApproveRegistrationRequest;
using T3.Application.Features.Registrations.ListRegistrationRequests;
using T3.Application.Features.Registrations.RejectRegistrationRequest;
using T3.Application.Features.Users;
using T3.Domain.Identity;
using T3.Domain.Startups;

namespace T3.Application.Tests;

/// <summary>
/// Yardımcı: bu dilimin onay/ret/liste handler'ları çağrılınca hiçbir izleme
/// çağrısı yapılmamalıysa (rol reddi veritabanından önce bitmeli) bu sınıf
/// patlar — <see cref="UnreachableDbContext"/>'in denetim izi karşılığı.
/// </summary>
internal sealed class UnreachableAuditWriter : IAuditWriter
{
    private static InvalidOperationException Unreachable() =>
        new("Bu karar denetim izine hiç ulaşmamalıydı.");

    public Task WriteAsync(
        string action, string entityType, Guid? entityId,
        object? before = null, object? after = null,
        CancellationToken ct = default, bool saveChanges = true) =>
        throw Unreachable();

    public Task WriteForActorAsync(
        Guid? actorUserId, UserRole? actorRole,
        string action, string entityType, Guid? entityId,
        object? before = null, object? after = null,
        CancellationToken ct = default, bool saveChanges = true) =>
        throw Unreachable();
}

/// <summary>
/// "Kayıt Ol" formunun gövde doğrulaması. <c>CreateUserValidator</c> ile aynı
/// kalıp: e-posta, ad, şifre kuralları admin eliyle açılan hesapla birebir
/// aynı — kendi kendine açılan hesabın güvenlik seviyesi düşük olamaz.
/// </summary>
public class RegisterStartupValidatorTests
{
    private static readonly RegisterStartupValidator Validator = new();

    private static readonly RegisterStartupRequest Valid = new(
        "yeni-girisim@ornek.test", "Guclu.Sifre1", "Elif Yıldırım",
        "Anadolu Robotik", Sector.Defense, "Ankara", "+905551112233");

    [Fact]
    public void Gecerli_basvuru_kabul_edilir()
    {
        Assert.True(Validator.Validate(Valid).IsValid);
    }

    [Fact]
    public void Eposta_zorunludur()
    {
        Assert.False(Validator.Validate(Valid with { Email = "" }).IsValid);
    }

    [Fact]
    public void Gecersiz_eposta_reddedilir()
    {
        Assert.False(Validator.Validate(Valid with { Email = "gecersiz" }).IsValid);
    }

    [Theory]
    [InlineData("kisa1")]          // 10 karakterden kısa
    [InlineData("sadeceharfler")]  // rakam yok
    [InlineData("1234567890")]     // harf yok
    public void Zayif_sifre_reddedilir(string password)
    {
        Assert.False(Validator.Validate(Valid with { Password = password }).IsValid);
    }

    [Fact]
    public void Girisim_adi_zorunludur()
    {
        Assert.False(Validator.Validate(Valid with { StartupName = "  " }).IsValid);
    }

    [Fact]
    public void Gecersiz_sektor_reddedilir()
    {
        Assert.False(Validator.Validate(Valid with { Sector = (Sector)999 }).IsValid);
    }
}

/// <summary>
/// Ret gerekçesi kuralı — <c>RejectChangeRequestValidator</c> ile birebir aynı
/// eşik (en az 10 karakter): kısa bir "olmaz" gerekçesi başvuru sahibine
/// hiçbir şey anlatmaz.
/// </summary>
public class RejectRegistrationRequestValidatorTests
{
    private static readonly RejectRegistrationRequestValidator Validator = new();

    [Fact]
    public void Bos_gerekce_reddedilir()
    {
        Assert.False(Validator.Validate(new RejectRegistrationRequestRequest("")).IsValid);
    }

    [Fact]
    public void Kisa_gerekce_reddedilir()
    {
        Assert.False(Validator.Validate(new RejectRegistrationRequestRequest("olmaz")).IsValid);
    }

    [Fact]
    public void Yeterli_uzunluktaki_gerekce_kabul_edilir()
    {
        Assert.True(Validator.Validate(
            new RejectRegistrationRequestRequest("Girişim adı belirsiz, lütfen tam unvanla tekrar başvurun.")).IsValid);
    }
}

/// <summary>
/// SuperAdmin dışı hiçbir rol bu uçlara dokunamaz — ve bu karar veritabanına
/// hiç gitmeden verilir. <see cref="UnreachableDbContext"/> ile
/// <see cref="UnreachableAuditWriter"/> bunu kanıtlanabilir kılıyor: reddeden
/// kod satırı yanlışlıkla sorgudan/izden SONRA taşınırsa test burada patlar.
/// Gerçek Startup+StartupUser satırının doğduğu onay yolu (Pending → Approve)
/// ise bu projede EF InMemory/Sqlite sağlayıcısı olmadığı için burada değil,
/// <c>scripts/e2e_faz*.py</c> uçtan uca betiklerinde gerçek Postgres'e karşı
/// doğrulanıyor.
/// </summary>
public class RegistrationRequestGuardTests
{
    [Theory]
    [InlineData(UserRole.ProgramManager)]
    [InlineData(UserRole.StartupUser)]
    [InlineData(UserRole.DecisionMaker)]
    public async Task Onay_yalnizca_SuperAdmine_acik(UserRole role)
    {
        var handler = new ApproveRegistrationRequestHandler(
            new UnreachableDbContext(),
            new UserAdminGuard(new UnreachableDbContext(), FakeCurrentUser.As(role)),
            FakeCurrentUser.As(role),
            new UnreachableAuditWriter());

        var result = await handler.Handle(Guid.NewGuid(), default);

        Assert.False(result.IsSuccess);
        Assert.Contains("yönetimi", result.Error!.Message);
    }

    [Theory]
    [InlineData(UserRole.ProgramManager)]
    [InlineData(UserRole.StartupUser)]
    [InlineData(UserRole.DecisionMaker)]
    public async Task Ret_yalnizca_SuperAdmine_acik(UserRole role)
    {
        var handler = new RejectRegistrationRequestHandler(
            new UnreachableDbContext(),
            new UserAdminGuard(new UnreachableDbContext(), FakeCurrentUser.As(role)),
            FakeCurrentUser.As(role),
            new UnreachableAuditWriter());

        var result = await handler.Handle(
            Guid.NewGuid(),
            new RejectRegistrationRequestRequest("Girişim bilgileri eksik, tekrar başvurunuz."),
            default);

        Assert.False(result.IsSuccess);
        Assert.Contains("yönetimi", result.Error!.Message);
    }

    [Theory]
    [InlineData(UserRole.ProgramManager)]
    [InlineData(UserRole.StartupUser)]
    [InlineData(UserRole.DecisionMaker)]
    public async Task Liste_yalnizca_SuperAdmine_acik(UserRole role)
    {
        var handler = new ListRegistrationRequestsHandler(
            new UnreachableDbContext(),
            new UserAdminGuard(new UnreachableDbContext(), FakeCurrentUser.As(role)));

        var result = await handler.Handle(new ListRegistrationRequestsRequest(), default);

        Assert.False(result.IsSuccess);
        Assert.Contains("yönetimi", result.Error!.Message);
    }
}
