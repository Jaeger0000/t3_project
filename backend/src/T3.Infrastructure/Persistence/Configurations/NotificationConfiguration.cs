using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using T3.Domain.Notifications;

namespace T3.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.Property(n => n.Message).HasMaxLength(2000).IsRequired();

        builder.HasOne(n => n.Startup)
            .WithMany()
            .HasForeignKey(n => n.StartupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Recipient)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.SentBy)
            .WithMany()
            .HasForeignKey(n => n.SentByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Alıcının gelen kutusu ("okunmamış" süzgeciyle) ve SuperAdmin'in
        // gönderen role göre ayrılmış gözetim ekranı bu iki indeksten geçiyor.
        builder.HasIndex(n => new { n.RecipientUserId, n.ReadAt });
        builder.HasIndex(n => n.SentByRole);

        // Kullanıcının soft-delete süzgeciyle eşleşen süzgeç (bkz.
        // PasswordResetTokenConfiguration ile aynı gerekçe): zorunlu
        // ilişkinin iki tarafı farklı süzgeç görürse EF uyarır — silinmiş
        // bir kullanıcının bildirimi sessizce "geçerli" sayılmamalı.
        builder.HasQueryFilter(n => !n.Recipient.IsDeleted && !n.SentBy.IsDeleted);
    }
}
