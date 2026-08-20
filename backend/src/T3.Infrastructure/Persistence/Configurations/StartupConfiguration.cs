using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using T3.Domain.Startups;

namespace T3.Infrastructure.Persistence.Configurations;

public class StartupConfiguration : IEntityTypeConfiguration<Startup>
{
    public void Configure(EntityTypeBuilder<Startup> builder)
    {
        builder.ToTable("Startups");

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.LegalName).HasMaxLength(300);
        builder.Property(s => s.TaxNumber).HasMaxLength(20);
        builder.Property(s => s.Website).HasMaxLength(300);
        builder.Property(s => s.LogoUrl).HasMaxLength(500);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.ContactEmail).HasMaxLength(256);
        builder.Property(s => s.ContactPhone).HasMaxLength(32);
        builder.Property(s => s.ProductDescription).HasMaxLength(4000);

        // Npgsql, List<string>'i yerel text[] kolonuna eşler.
        builder.Property(s => s.TechnologyAreas).HasColumnType("text[]");

        builder.HasIndex(s => s.Name);
        builder.HasIndex(s => s.Sector);
        builder.HasIndex(s => s.Status);
    }
}
