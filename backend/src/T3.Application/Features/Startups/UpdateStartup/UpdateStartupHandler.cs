using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Domain.Startups;

namespace T3.Application.Features.Startups.UpdateStartup;

public sealed record UpdateStartupResponse(Guid Id, string Name, DateTimeOffset? UpdatedAt);

public sealed class UpdateStartupHandler(
    IAppDbContext db,
    IStartupScope scope,
    IAuditWriter audit)
{
    public async Task<Result<UpdateStartupResponse>> Handle(
        Guid id, StartupWriteModel model, CancellationToken ct)
    {
        // Kapsam kontrolü önce: kullanıcı göremediği bir girişimi
        // düzenleyemez, varlığını da öğrenemez.
        var startup = await scope.Apply(db.Startups)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (startup is null)
            return Error.NotFound("Girişim bulunamadı.");

        if (!scope.CanEditDirectly(startup.Id))
            return Error.Forbidden(
                "Doğrudan düzenleme yetkiniz yok. Değişikliğinizi onay isteği olarak gönderin.");

        var name = model.Name.Trim();
        var normalized = SearchText.Normalize(name);

        var nameTaken = await db.Startups
            .AnyAsync(s => s.Id != id && s.Name.ToLower() == normalized, ct);

        if (nameTaken)
            return Error.Conflict($"\"{name}\" adlı başka bir girişim zaten kayıtlı.");

        var before = StartupAuditSnapshot.Of(startup);

        model.ApplyTo(startup);
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            "Startup.Update", nameof(Startup), startup.Id,
            before: before,
            after: StartupAuditSnapshot.Of(startup),
            ct: ct);

        return new UpdateStartupResponse(startup.Id, startup.Name, startup.UpdatedAt);
    }
}
