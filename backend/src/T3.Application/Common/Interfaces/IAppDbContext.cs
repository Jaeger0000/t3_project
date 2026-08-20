using Microsoft.EntityFrameworkCore;
using T3.Domain.Achievements;
using T3.Domain.Approvals;
using T3.Domain.Audit;
using T3.Domain.Documents;
using T3.Domain.Identity;
using T3.Domain.Milestones;
using T3.Domain.Programs;
using T3.Domain.Startups;

namespace T3.Application.Common.Interfaces;

/// <summary>
/// Application katmanının veritabanı görüşü. Somut DbContext Infrastructure'da
/// yaşar; handler'lar yalnızca bu arayüzü bilir.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserProgramAssignment> UserProgramAssignments { get; }

    DbSet<Startup> Startups { get; }
    DbSet<TeamMember> TeamMembers { get; }

    DbSet<EcosystemProgram> Programs { get; }
    DbSet<ProgramTerm> ProgramTerms { get; }
    DbSet<ProgramParticipation> ProgramParticipations { get; }

    DbSet<Achievement> Achievements { get; }
    DbSet<Document> Documents { get; }
    DbSet<Milestone> Milestones { get; }
    DbSet<ChangeRequest> ChangeRequests { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
