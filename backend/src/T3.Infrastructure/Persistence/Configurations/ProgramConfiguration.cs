using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using T3.Domain.Programs;

namespace T3.Infrastructure.Persistence.Configurations;

public class EcosystemProgramConfiguration : IEntityTypeConfiguration<EcosystemProgram>
{
    public void Configure(EntityTypeBuilder<EcosystemProgram> builder)
    {
        builder.ToTable("Programs");

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Coordinatorship).HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(2000);

        builder.HasIndex(p => p.Name).IsUnique();
    }
}

public class ProgramTermConfiguration : IEntityTypeConfiguration<ProgramTerm>
{
    public void Configure(EntityTypeBuilder<ProgramTerm> builder)
    {
        builder.ToTable("ProgramTerms");

        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();

        builder.HasOne(t => t.Program)
            .WithMany(p => p.Terms)
            .HasForeignKey(t => t.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.ProgramId, t.Name }).IsUnique();
    }
}

public class ProgramParticipationConfiguration : IEntityTypeConfiguration<ProgramParticipation>
{
    public void Configure(EntityTypeBuilder<ProgramParticipation> builder)
    {
        builder.ToTable("ProgramParticipations");

        builder.Property(p => p.Notes).HasMaxLength(2000);

        builder.HasOne(p => p.Startup)
            .WithMany(s => s.Participations)
            .HasForeignKey(p => p.StartupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.ProgramTerm)
            .WithMany(t => t.Participations)
            .HasForeignKey(p => p.ProgramTermId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bir girişim aynı döneme iki kez katılamaz.
        builder.HasIndex(p => new { p.StartupId, p.ProgramTermId }).IsUnique();
    }
}
