namespace T3.Domain.Common;

/// <summary>Oluşturma/güncelleme zamanı DbContext tarafından otomatik doldurulur.</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
}
