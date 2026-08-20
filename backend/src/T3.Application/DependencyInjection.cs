using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using T3.Application.Common.Rbac;
using T3.Application.Features.Startups;

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
