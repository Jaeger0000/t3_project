namespace T3.Domain.Common;

/// <summary>Tüm kalıcı varlıkların ortak temeli.</summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
