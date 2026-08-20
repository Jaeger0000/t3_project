using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using T3.Domain.Approvals;

namespace T3.Infrastructure.Persistence.Configurations;

public class ChangeRequestConfiguration : IEntityTypeConfiguration<ChangeRequest>
{
    public void Configure(EntityTypeBuilder<ChangeRequest> builder)
    {
        builder.ToTable("ChangeRequests");

        // Öneri gövdeleri Postgres jsonb olarak saklanır — sorgulanabilir kalır.
        builder.Property(c => c.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(c => c.BeforeJson).HasColumnType("jsonb");
        builder.Property(c => c.ReviewNote).HasMaxLength(2000);

        builder.HasOne(c => c.Startup)
            .WithMany()
            .HasForeignKey(c => c.StartupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.SubmittedBy)
            .WithMany()
            .HasForeignKey(c => c.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ReviewedBy)
            .WithMany()
            .HasForeignKey(c => c.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Onay kuyruğunun ana sorgusu: bekleyenler, en eski önce.
        builder.HasIndex(c => new { c.Status, c.SubmittedAt });
        builder.HasIndex(c => c.StartupId);
    }
}
