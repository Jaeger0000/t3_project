using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace T3.Infrastructure.Persistence;

/// <summary>
/// Migration komutlarının DbContext'i tüm uygulama host'unu ayağa kaldırmadan
/// oluşturabilmesi için design-time fabrikası. Bağlantı dizesini ortam
/// değişkeninden okur; yoksa yerel geliştirme varsayılanına düşer.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("T3_ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=t3ekosistem;Username=t3;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
