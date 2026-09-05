using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using T3.Domain.Registrations;

namespace T3.Infrastructure.Persistence.Configurations;

public class RegistrationConfiguration : IEntityTypeConfiguration<StartupRegistrationRequest>
{
    public void Configure(EntityTypeBuilder<StartupRegistrationRequest> builder)
    {
        builder.ToTable("StartupRegistrationRequests");

        builder.Property(r => r.Email).HasMaxLength(256).IsRequired();
        builder.Property(r => r.PasswordHash).IsRequired();
        builder.Property(r => r.FullName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.StartupName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.City).HasMaxLength(100);
        builder.Property(r => r.ContactPhone).HasMaxLength(32);
        builder.Property(r => r.RejectionReason).HasMaxLength(2000);

        // E-posta üzerinden "zaten bekleyen bir başvuru var mı" kontrolü sık
        // çalışıyor; durum da liste süzgecinde kullanılıyor.
        builder.HasIndex(r => r.Email);
        builder.HasIndex(r => r.Status);
    }
}
