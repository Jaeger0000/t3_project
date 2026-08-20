using Microsoft.Extensions.Options;
using T3.Application.Common.Interfaces;

namespace T3.Infrastructure.Storage;

public class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    public string RootPath { get; set; } = "storage/documents";
}

/// <summary>
/// Geliştirme ve demo için diske yazan uygulama. S3 uyumlu sürüm aynı arayüzü
/// uygular; handler'lar değişmez.
/// </summary>
public sealed class LocalDocumentStorage(IOptions<DocumentStorageOptions> options) : IDocumentStorage
{
    private readonly string _root = Path.GetFullPath(options.Value.RootPath);

    public async Task<string> SaveAsync(
        Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        // Tarih bazlı klasörleme + rastgele ad: kullanıcı dosya adı yola girmez,
        // böylece path traversal ve isim çakışması riski kalmaz.
        var relativeDir = Path.Combine(
            DateTime.UtcNow.ToString("yyyy"),
            DateTime.UtcNow.ToString("MM"));

        var extension = Path.GetExtension(fileName);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(relativeDir, storedName);

        var absoluteDir = Path.Combine(_root, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        await using var target = File.Create(Path.Combine(_root, relativePath));
        await content.CopyToAsync(target, ct);

        return relativePath;
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default)
    {
        var absolute = ResolveInsideRoot(storagePath);

        if (!File.Exists(absolute))
            throw new FileNotFoundException("Doküman bulunamadı.", storagePath);

        return Task.FromResult<Stream>(File.OpenRead(absolute));
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var absolute = ResolveInsideRoot(storagePath);

        if (File.Exists(absolute))
            File.Delete(absolute);

        return Task.CompletedTask;
    }

    /// <summary>Çözülen yolun depo kökünün dışına çıkmadığını doğrular.</summary>
    private string ResolveInsideRoot(string storagePath)
    {
        var absolute = Path.GetFullPath(Path.Combine(_root, storagePath));

        if (!absolute.StartsWith(_root, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Geçersiz doküman yolu.");

        return absolute;
    }
}
