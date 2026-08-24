using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Domain.Achievements;
using T3.Domain.Approvals;
using T3.Domain.Audit;
using T3.Domain.Documents;
using T3.Domain.Identity;
using T3.Domain.Milestones;
using T3.Domain.Programs;
using T3.Domain.Startups;

namespace T3.Application.Tests;

/// <summary>
/// Her erişimde patlayan veritabanı görüşü. Yetki kararlarının bir kısmı
/// yalnızca role bakar ve sorgu çalıştırmamalı; bu sınıf o beklentiyi
/// kanıtlanabilir kılıyor — sessizce çalışan bir sorgu testi düşürür.
/// </summary>
internal sealed class UnreachableDbContext : IAppDbContext
{
    private static InvalidOperationException Unreachable(string set) =>
        new($"Bu karar veritabanına gitmemeliydi; '{set}' okundu.");

    public DbSet<User> Users => throw Unreachable(nameof(Users));
    public DbSet<UserProgramAssignment> UserProgramAssignments =>
        throw Unreachable(nameof(UserProgramAssignments));
    public DbSet<Startup> Startups => throw Unreachable(nameof(Startups));
    public DbSet<TeamMember> TeamMembers => throw Unreachable(nameof(TeamMembers));
    public DbSet<EcosystemProgram> Programs => throw Unreachable(nameof(Programs));
    public DbSet<ProgramTerm> ProgramTerms => throw Unreachable(nameof(ProgramTerms));
    public DbSet<ProgramParticipation> ProgramParticipations =>
        throw Unreachable(nameof(ProgramParticipations));
    public DbSet<Achievement> Achievements => throw Unreachable(nameof(Achievements));
    public DbSet<Document> Documents => throw Unreachable(nameof(Documents));
    public DbSet<Milestone> Milestones => throw Unreachable(nameof(Milestones));
    public DbSet<ChangeRequest> ChangeRequests => throw Unreachable(nameof(ChangeRequests));
    public DbSet<AuditLog> AuditLogs => throw Unreachable(nameof(AuditLogs));

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw Unreachable(nameof(SaveChangesAsync));
}
