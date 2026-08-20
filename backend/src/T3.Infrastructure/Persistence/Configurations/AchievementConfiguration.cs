using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using T3.Domain.Achievements;

namespace T3.Infrastructure.Persistence.Configurations;

/// <summary>
/// Başarı/finansal kayıtları TPH (Table Per Hierarchy) ile tek tabloda tutar:
/// C# tarafında güçlü tipli sınıflar, veritabanında "Kind" ayrıştırıcı kolonu.
/// Böylece brief'in istediği "alan bazlı, serbest metin olmayan" model
/// tek sorguda okunabilir kalıyor.
///
/// Not: her özellik onu *tanımlayan* tipin yapılandırmasında ayarlanır;
/// türetilmiş bir özelliği temel tipte ayarlamak EF'te gölge özellik hatasına yol açar.
/// </summary>
public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.ToTable("Achievements");

        builder.HasDiscriminator<string>("Kind")
            .HasValue<RevenueRecord>("Revenue")
            .HasValue<ExportRecord>("Export")
            .HasValue<InvestmentRound>("Investment")
            .HasValue<GrantRecord>("Grant")
            .HasValue<AwardRecord>("Award");

        builder.Property(a => a.Note).HasMaxLength(1000);

        builder.HasOne(a => a.Startup)
            .WithMany(s => s.Achievements)
            .HasForeignKey(a => a.StartupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.StartupId, a.OccurredOn });
    }
}

/// <summary>Tutar taşıyan tüm kayıtların ortak alanları (tek TPH kolonu).</summary>
public class MoneyAchievementConfiguration : IEntityTypeConfiguration<MoneyAchievement>
{
    public void Configure(EntityTypeBuilder<MoneyAchievement> builder)
    {
        builder.Property(a => a.Amount).HasPrecision(18, 2);
        builder.Property(a => a.Currency).HasMaxLength(3).IsRequired();
    }
}

public class InvestmentRoundConfiguration : IEntityTypeConfiguration<InvestmentRound>
{
    public void Configure(EntityTypeBuilder<InvestmentRound> builder)
    {
        builder.Property(r => r.Valuation).HasPrecision(18, 2);
        builder.Property(r => r.InvestorNames).HasColumnType("text[]");
    }
}

public class ExportRecordConfiguration : IEntityTypeConfiguration<ExportRecord>
{
    public void Configure(EntityTypeBuilder<ExportRecord> builder)
    {
        builder.Property(r => r.TargetCountries).HasColumnType("text[]");
    }
}

public class GrantRecordConfiguration : IEntityTypeConfiguration<GrantRecord>
{
    public void Configure(EntityTypeBuilder<GrantRecord> builder)
    {
        builder.Property(r => r.ProgramName).HasMaxLength(300);
    }
}

public class AwardRecordConfiguration : IEntityTypeConfiguration<AwardRecord>
{
    public void Configure(EntityTypeBuilder<AwardRecord> builder)
    {
        builder.Property(r => r.Name).HasMaxLength(300).IsRequired();
        builder.Property(r => r.Organization).HasMaxLength(300);
    }
}
