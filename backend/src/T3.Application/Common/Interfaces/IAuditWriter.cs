namespace T3.Application.Common.Interfaces;

/// <summary>Denetim izi yazıcısı — her yazma işleminden sonra çağrılır.</summary>
public interface IAuditWriter
{
    Task WriteAsync(
        string action,
        string entityType,
        Guid? entityId,
        object? before = null,
        object? after = null,
        CancellationToken ct = default);
}
