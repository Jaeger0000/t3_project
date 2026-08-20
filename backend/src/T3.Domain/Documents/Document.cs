using T3.Domain.Common;
using T3.Domain.Startups;

namespace T3.Domain.Documents;

public enum DocumentType
{
    Other = 0,
    PitchDeck = 1,
    Financials = 2,
    Incorporation = 3,
    Patent = 4,
    Report = 5,
    Contract = 6
}

/// <summary>
/// Girişim dokümanı. Dosyanın kendisi veritabanında tutulmaz;
/// <c>StoragePath</c> IDocumentStorage üzerinden çözülür ve indirme
/// bağlantıları kısa ömürlü imzalı URL olarak üretilir.
/// </summary>
public class Document : Entity, IAuditable, ISoftDelete
{
    public Guid StartupId { get; set; }
    public Startup Startup { get; set; } = null!;

    public DocumentType Type { get; set; }
    public string FileName { get; set; } = null!;
    public string StoragePath { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }

    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset UploadedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
