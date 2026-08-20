using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using T3.Domain.Identity;

namespace T3.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasOne(u => u.Startup)
            .WithMany()
            .HasForeignKey(u => u.StartupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class UserProgramAssignmentConfiguration : IEntityTypeConfiguration<UserProgramAssignment>
{
    public void Configure(EntityTypeBuilder<UserProgramAssignment> builder)
    {
        builder.ToTable("UserProgramAssignments");

        builder.HasOne(a => a.User)
            .WithMany(u => u.ProgramAssignments)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Program)
            .WithMany(p => p.ManagerAssignments)
            .HasForeignKey(a => a.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        // Aynı kullanıcı aynı programa iki kez atanamaz.
        builder.HasIndex(a => new { a.UserId, a.ProgramId }).IsUnique();
    }
}
