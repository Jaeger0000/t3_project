using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Domain.Achievements;
using T3.Domain.Approvals;
using T3.Domain.Audit;
using T3.Domain.Common;
using T3.Domain.Documents;
using T3.Domain.Identity;
using T3.Domain.Milestones;
using T3.Domain.Programs;
using T3.Domain.Startups;

namespace T3.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProgramAssignment> UserProgramAssignments => Set<UserProgramAssignment>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<Startup> Startups => Set<Startup>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    public DbSet<EcosystemProgram> Programs => Set<EcosystemProgram>();
    public DbSet<ProgramTerm> ProgramTerms => Set<ProgramTerm>();
    public DbSet<ProgramParticipation> ProgramParticipations => Set<ProgramParticipation>();

    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<ChangeRequest> ChangeRequests => Set<ChangeRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplySoftDeleteFilters(modelBuilder);
    }

    /// <summary>
    /// ISoftDelete uygulayan her varlığa global sorgu filtresi ekler; silinmiş
    /// kayıtlar normal sorgularda görünmez (IgnoreQueryFilters ile erişilebilir).
    /// </summary>
    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                continue;

            // TPH hiyerarşisinde filtre yalnızca kök tipe tanımlanabilir.
            if (entityType.BaseType is not null)
                continue;

            var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
            var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            var filter = System.Linq.Expressions.Expression.Lambda(
                System.Linq.Expressions.Expression.Not(property), parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>CreatedAt/UpdatedAt ve soft delete zamanını merkezi olarak doldurur.</summary>
    private void StampTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        // Açıkça verilmiş tarih korunur: tohum veri ve dışarıdan
                        // aktarılan kayıtlar gerçek geçmiş tarihlerini taşıyabilsin.
                        if (auditable.CreatedAt == default)
                            auditable.CreatedAt = now;
                        break;
                    case EntityState.Modified:
                        auditable.UpdatedAt = now;
                        break;
                }
            }

            if (entry is { State: EntityState.Modified, Entity: ISoftDelete deletable }
                && deletable is { IsDeleted: true, DeletedAt: null })
            {
                deletable.DeletedAt = now;
            }
        }
    }
}
