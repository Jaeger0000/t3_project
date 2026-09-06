using Microsoft.EntityFrameworkCore;
using T3.Domain.Achievements;
using T3.Domain.Approvals;
using T3.Domain.Assistant;
using T3.Domain.Audit;
using T3.Domain.Documents;
using T3.Domain.Identity;
using T3.Domain.Milestones;
using T3.Domain.Notifications;
using T3.Domain.Programs;
using T3.Domain.Registrations;
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
    DbSet<PasswordResetToken> PasswordResetTokens { get; }

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

    /// <summary>Kayıt Ol ekranından gelen, onay bekleyen girişim başvuruları.</summary>
    DbSet<StartupRegistrationRequest> StartupRegistrationRequests { get; }

    /// <summary>Sunucuda saklanan AI sohbetleri ve turları (bkz. Features/Assistant/Chat).</summary>
    DbSet<AiConversation> AiConversations { get; }
    DbSet<AiConversationMessage> AiConversationMessages { get; }

    /// <summary>Yöneticiden girişime giden bildirimler (bkz. Features/Notifications).</summary>
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
