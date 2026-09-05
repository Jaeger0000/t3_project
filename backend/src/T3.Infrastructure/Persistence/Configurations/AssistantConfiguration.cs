using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using T3.Domain.Assistant;
using T3.Domain.Identity;

namespace T3.Infrastructure.Persistence.Configurations;

public class AiConversationConfiguration : IEntityTypeConfiguration<AiConversation>
{
    public void Configure(EntityTypeBuilder<AiConversation> builder)
    {
        builder.ToTable("AiConversations");

        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();

        // Kullanıcı satırı gerçekten silinirse sohbetleri de gitmeli: sohbet
        // metni sahibinin kişisel verisi, sahipsiz kalması KVKK açısından
        // savunulamaz. Navigasyon özelliği yok, ilişki gölge olarak kuruluyor.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Sohbet listesinin tek sorgusu: "benim sohbetlerim, en son konuşulan
        // önce".
        builder.HasIndex(c => new { c.OwnerUserId, c.LastMessageAt });
    }
}

public class AiConversationMessageConfiguration : IEntityTypeConfiguration<AiConversationMessage>
{
    public void Configure(EntityTypeBuilder<AiConversationMessage> builder)
    {
        builder.ToTable("AiConversationMessages");

        builder.Property(m => m.Text).HasMaxLength(8000).IsRequired();
        builder.Property(m => m.ToolNamesJson).HasMaxLength(2000);
        builder.Property(m => m.StartupIdsJson).HasMaxLength(2000);
        builder.Property(m => m.Mode).HasMaxLength(32);
        builder.Property(m => m.ModelName).HasMaxLength(120);

        // Sohbet silindiğinde mesajlar da silinir: saklama süresi temizliği
        // yalnızca başlığı silip metni bırakırsa iş görmez.
        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt });
    }
}
