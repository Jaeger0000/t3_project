namespace T3.Domain.Common;

/// <summary>
/// Kayıt silinmez, işaretlenir. KVKK açısından silme talebi ayrı bir
/// anonimleştirme akışıyla karşılanır; buradaki amaç denetim izini korumak.
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}
