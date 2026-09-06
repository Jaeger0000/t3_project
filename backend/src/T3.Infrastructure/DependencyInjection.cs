using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using T3.Application.Common.Interfaces;
using T3.Infrastructure.Ai;
using T3.Infrastructure.Assistant;
using T3.Infrastructure.Audit;
using T3.Infrastructure.Identity;
using T3.Infrastructure.Notifications;
using T3.Infrastructure.Persistence;
using T3.Infrastructure.Persistence.Seed;
using T3.Infrastructure.Reports;
using T3.Infrastructure.Storage;

namespace T3.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
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
        // IAppDbContext scoped olduğu için bu da scoped: singleton olsaydı
        // scoped bağımlılığı istek boyunca yakalardı. Asıl önbellekleme
        // singleton IMemoryCache üzerinden zaten oturumlar arasında paylaşılıyor.
        services.AddScoped<IUserStateProvider, UserStateProvider>();
        services.AddHostedService<RetentionCleanupService>();

        AddEmailSender(services, configuration, environment);
        services.AddSingleton<IResetLinkBuilder, ResetLinkBuilder>();
        services.AddSingleton<IAppLinkBuilder, AppLinkBuilder>();

        AddChatModel(services, configuration);
        services.AddSingleton<IReportPdfRenderer, QuestPdfReportRenderer>();
        services.AddSingleton<IExcelFileBuilder, ClosedXmlExcelFileBuilder>();

        // AI asistanının ürettiği indirilebilir dosyalar (ör. Excel dışa
        // aktarma) sohbet protokolü ikili veri taşıyamadığı için burada
        // bekletiliyor — bkz. IAssistantExportStore.
        services.AddSingleton<IAssistantExportStore, InMemoryAssistantExportStore>();

        return services;
    }

    /// <summary>
    /// Gönderici seçimi **ortama değil, yapılandırmaya** bakar: SMTP bilgileri
    /// tamsa (sunucu + kullanıcı + parola) gerçek gönderici kayıtlanır, aksi
    /// hâlde ortam kararı verir. Sıralamanın böyle olması, SMTP'yi geliştirme
    /// makinesinde de deneyebilmek için gerekli — "yalnızca üretimde çalışan
    /// yol" ilk kez canlıda denenmiş olurdu.
    ///
    /// SMTP yokken üretimde <see cref="ThrowingEmailSender"/> kalıyor:
    /// <see cref="FileOutboxEmailSender"/> ham şifre sıfırlama jetonunu
    /// sunucu diskine ikinci bir düz metin kopyası olarak yazardı
    /// (bkz. G-02, docs/Guvenlik_Denetimi_ve_Iyilestirme_Plani.md), sessiz bir
    /// "hiçbir şey yapma" göndericisi ise arızayı görünmez kılardı.
    /// </summary>
    private static void AddEmailSender(
        IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var email = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>()
            ?? new EmailOptions();

        if (email.Smtp.IsConfigured)
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else if (environment.IsProduction())
            services.AddScoped<IEmailSender, ThrowingEmailSender>();
        else
            services.AddScoped<IEmailSender, FileOutboxEmailSender>();
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

        // Araç desteği süreç ömrü boyunca hatırlanıyor; singleton olmak zorunda.
        services.AddSingleton<AiCapabilityState>();

        if (ai.ResolveProvider() == AiProvider.Anthropic)
        {
            services.AddHttpClient<IChatModel, AnthropicChatModel>(client =>
            {
                client.BaseAddress = new Uri(ai.BaseUrl ?? "https://api.anthropic.com");
                client.Timeout = TimeSpan.FromSeconds(ai.TimeoutSeconds);
                client.DefaultRequestHeaders.Add("x-api-key", ai.ApiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            });

            return;
        }

        services.AddHttpClient<IChatModel, OpenRouterChatModel>(client =>
        {
            client.BaseAddress = new Uri(ai.BaseUrl ?? "https://openrouter.ai");
            client.Timeout = TimeSpan.FromSeconds(ai.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ai.ApiKey}");

            // OpenRouter'ın kullanım sayfasında isteğin hangi uygulamadan
            // geldiğini göstermek için; zorunlu değil, tanımlıysa gönderiliyor.
            if (!string.IsNullOrWhiteSpace(ai.SiteUrl))
                client.DefaultRequestHeaders.Add("HTTP-Referer", ai.SiteUrl);

            if (!string.IsNullOrWhiteSpace(ai.AppTitle))
                client.DefaultRequestHeaders.Add("X-Title", ai.AppTitle);
        });
    }
}
