using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using T3.Domain.Audit;
using T3.Domain.Documents;
using T3.Domain.Milestones;
using T3.Domain.Startups;

namespace T3.Infrastructure.Persistence.Configurations;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("TeamMembers");

        builder.Property(m => m.FullName).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Title).HasMaxLength(120);
        builder.Property(m => m.Email).HasMaxLength(256);
        builder.Property(m => m.Phone).HasMaxLength(32);
        builder.Property(m => m.LinkedInUrl).HasMaxLength(300);

        builder.HasOne(m => m.Startup)
            .WithMany(s => s.TeamMembers)
            .HasForeignKey(m => m.StartupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.Property(d => d.FileName).HasMaxLength(300).IsRequired();
        builder.Property(d => d.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(120).IsRequired();

        builder.HasOne(d => d.Startup)
            .WithMany(s => s.Documents)
            .HasForeignKey(d => d.StartupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.StartupId, d.Type });
    }
}

public class MilestoneConfiguration : IEntityTypeConfiguration<Milestone>
{
    public void Configure(EntityTypeBuilder<Milestone> builder)
    {
        builder.ToTable("Milestones");

        builder.Property(m => m.Title).HasMaxLength(300).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(2000);

        builder.HasOne(m => m.Startup)
            .WithMany(s => s.Milestones)
            .HasForeignKey(m => m.StartupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.StartupId, m.OccurredOn });
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.Property(a => a.Action).HasMaxLength(120).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(120).IsRequired();
        builder.Property(a => a.IpAddress).HasMaxLength(64);
        builder.Property(a => a.BeforeJson).HasColumnType("jsonb");
        builder.Property(a => a.AfterJson).HasColumnType("jsonb");

        builder.HasIndex(a => a.OccurredAt);
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}
