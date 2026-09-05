using Microsoft.Extensions.Options;
using T3.Infrastructure.Storage;

namespace T3.Application.Tests;

/// <summary>
/// Depo kökünün dışına çıkan bir yol IDOR/traversal'a açık kapı — bu testler
/// <c>ResolveInsideRoot</c>'un ayırıcısız <c>StartsWith</c> yerine gerçek bir
/// göreli yol kontrolü yaptığını kilitliyor (bkz. G-13).
/// </summary>
public class LocalDocumentStorageTests : IDisposable
{
    private readonly string _root;
    private readonly LocalDocumentStorage _storage;

    public LocalDocumentStorageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"t3-storage-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);

        _storage = new LocalDocumentStorage(
            Options.Create(new DocumentStorageOptions { RootPath = _root }));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Theory]
    [InlineData("../gizli.pdf")]
    [InlineData("../../etc/passwd")]
    [InlineData("2026/../../gizli.pdf")]
    public async Task Kok_disina_cikan_goreli_yol_reddedilir(string traversal)
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _storage.OpenReadAsync(traversal));
    }

    [Fact]
    public async Task Mutlak_yol_reddedilir()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _storage.OpenReadAsync("/etc/passwd"));
    }

    /// <summary>
    /// Ayırıcısız <c>StartsWith</c> kontrolü "kök + kardeş klasör önekini
    /// paylaşan" bir yolu da kök içi sayardı — asıl bulgu bu.
    /// </summary>
    [Fact]
    public async Task Kok_onekini_paylasan_kardes_klasor_reddedilir()
    {
        var siblingDir = _root + "-yedek";
        Directory.CreateDirectory(siblingDir);
        var siblingFile = Path.Combine(siblingDir, "sizinti.pdf");
        await File.WriteAllTextAsync(siblingFile, "gizli");

        try
        {
            var relativeFromRoot = Path.GetRelativePath(_root, siblingFile);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _storage.OpenReadAsync(relativeFromRoot));
        }
        finally
        {
            Directory.Delete(siblingDir, recursive: true);
        }
    }

    [Fact]
    public async Task Kok_icindeki_gecerli_yol_kabul_edilir()
    {
        var relativeDir = Path.Combine("2026", "09");
        Directory.CreateDirectory(Path.Combine(_root, relativeDir));
        var relativePath = Path.Combine(relativeDir, "belge.pdf");
        await File.WriteAllTextAsync(Path.Combine(_root, relativePath), "içerik");

        await using var stream = await _storage.OpenReadAsync(relativePath);

        Assert.True(stream.CanRead);
    }
}
