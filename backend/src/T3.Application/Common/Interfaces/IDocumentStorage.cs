namespace T3.Application.Common.Interfaces;

/// <summary>
/// Doküman deposu. Yerelde diske, demoda S3 uyumlu servise yazan iki uygulaması
/// var; handler'lar hangisinin devrede olduğunu bilmez.
/// </summary>
public interface IDocumentStorage
{
    /// <summary>Dosyayı yazar ve geri okunabilir depo yolunu döner.</summary>
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);

    Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default);

    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}
