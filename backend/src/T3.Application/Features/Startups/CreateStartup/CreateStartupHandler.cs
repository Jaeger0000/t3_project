using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Rbac;
using T3.Application.Common.Results;
using T3.Application.Common.Text;
using T3.Domain.Startups;

namespace T3.Application.Features.Startups.CreateStartup;

public sealed record CreateStartupResponse(Guid Id, string Name);

public sealed class CreateStartupHandler(
    IAppDbContext db,
    IStartupScope scope,
    IAuditWriter audit)
{
    public async Task<Result<CreateStartupResponse>> Handle(
        StartupWriteModel model, CancellationToken ct)
    {
        if (!scope.CanCreateStartups)
            return Error.Forbidden("Yeni girişim ekleme yetkiniz yok.");

        var name = model.Name.Trim();
        var normalized = SearchText.Normalize(name);

        // Aynı adı iki kez kaydetmek "tek doğrulanmış kayıt" ilkesini bozar.
        var exists = await db.Startups
            .IgnoreQueryFilters()
            .AnyAsync(s => s.Name.ToLower() == normalized && !s.IsDeleted, ct);

        if (exists)
            return Error.Conflict($"\"{name}\" adlı bir girişim zaten kayıtlı.");

        var startup = new Startup();
        model.ApplyTo(startup);
        startup.Status = model.Status ?? StartupStatus.Active;

        db.Startups.Add(startup);
        await db.SaveChangesAsync(ct);

        await audit.WriteAsync(
            "Startup.Create", nameof(Startup), startup.Id,
            after: StartupAuditSnapshot.Of(startup), ct: ct);

        return new CreateStartupResponse(startup.Id, startup.Name);
    }
}
