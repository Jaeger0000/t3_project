using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using T3.Application.Common.Rbac;
using T3.Application.Features.Approvals;
using T3.Application.Features.Programs;
using T3.Application.Features.Assistant;
using T3.Application.Features.Startups;
using T3.Application.Features.Users;

namespace T3.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IStartupScope, StartupScope>();

        // Dilimler arası paylaşılan yetki muhafızı. İsimlendirme kuralına
        // uymadığı için elle kaydediliyor; "*Handler" adı vermek işini yanlış
        // anlatırdı — bu bir use-case değil, ortak bir ön kontrol.
        services.AddScoped<StartupEditGuard>();

        // Onay akışının iki parçası. Kapsam, satır düzeyi yetkiyi tek noktada
        // tutar; uygulayıcı onaylanan öneriyi hedef varlığa işler. İkisi de
        // "*Handler" kuralına girmiyor: use-case değil, dilimler arası ortak
        // bileşenler.
        services.AddScoped<IChangeRequestScope, ChangeRequestScope>();
        services.AddScoped<ChangeRequestApplier>();

        // Kullanıcı yönetiminin ortak ön kontrolleri (rol-kapsam bağı, e-posta
        // tekilliği, program ataması eşitleme).
        services.AddScoped<UserAdminGuard>();

        // Program dilimlerinin ortak yetki kapısı: program tanımı SuperAdmin'e,
        // dönem/katılım kendi programına sahip yöneticiye açık.
        services.AddScoped<ProgramAccessGuard>();

        // AI ve MCP'nin ortak araç kutusu ile modelsiz çalışan yedek planlayıcı.
        // İkisi de use-case değil; sohbet ve MCP uçları bunları sarar.
        services.AddScoped<AssistantToolbox>();
        services.AddScoped<OfflineAssistant>();

        // Ajan döngüsü: tek soru ucu ile kalıcı sohbet dilimi aynı bileşeni
        // çağırır. "*Handler" değil çünkü use-case değil — döngüyü kopyalamak,
        // araç sonuçlarının modele giderken süzülmesi gibi adımların yalnızca
        // bir dilimde güncellenmesi riskini doğururdu.
        services.AddScoped<AssistantConversationRunner>();

        // Dikey dilim handler'ları: Features altındaki *Handler sınıflarını
        // isimlendirme kuralına göre otomatik kaydeder, böylece her yeni
        // özellik için DI'a elle satır eklemek gerekmez.
        var handlers = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && t.Name.EndsWith("Handler", StringComparison.Ordinal));

        foreach (var handler in handlers)
            services.AddScoped(handler);

        return services;
    }
}
