using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
namespace Infrastructure;

public static class DependencyInjection {
    public static IServiceCollection AddInfrastructure(this IServiceCollection services) {
        services.AddSingleton<SettingsService>();
        services.AddSingleton<AuthService>();
        services.AddSingleton<AuthDataService>();
        return services;
    }
}