using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using T3.Application.Common.Interfaces;
using T3.Infrastructure.Ai;
using T3.Infrastructure.Audit;
using T3.Infrastructure.Identity;
using T3.Infrastructure.Notifications;
using T3.Infrastructure.Persistence;
using T3.Infrastructure.Persistence.Seed;
using T3.Infrastructure.Storage;

namespace T3.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default tanımlı değil. .env dosyasını kontrol edin.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<DocumentStorageOptions>(
            configuration.GetSection(DocumentStorageOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.AddScoped<DevDataSeeder>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IDocumentStorage, LocalDocumentStorage>();
        services.AddScoped<IAuditWriter, AuditWriter>();

        // Gerçek SMTP Creathon kapsamında yok; gönderici e-postayı diske yazan
        // geliştirme kutusuna düşüyor (bkz. FileOutboxEmailSender).
        services.AddScoped<IEmailSender, FileOutboxEmailSender>();
        services.AddSingleton<IResetLinkBuilder, ResetLinkBuilder>();

        AddChatModel(services, configuration);

        return services;
    }

    /// <summary>
    /// Dil modeli yalnızca anahtar tanımlıysa gerçek sağlayıcıya bağlanır.
    /// Anahtar yoksa devre dışı model kaydedilir ve sohbet ucu yerel plana
    /// düşer — sistem ayağa kalkmayı reddetmez, çünkü AI zorunlu MVP maddesi
    /// değil, karar destek eklentisi.
    /// </summary>
    private static void AddChatModel(IServiceCollection services, IConfiguration configuration)
    {
        var ai = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();

        if (string.IsNullOrWhiteSpace(ai.ApiKey))
        {
            services.AddSingleton<IChatModel, DisabledChatModel>();
            return;
        }

        services.AddHttpClient<IChatModel, AnthropicChatModel>(client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com");
            client.Timeout = TimeSpan.FromSeconds(ai.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("x-api-key", ai.ApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        });
    }
}
