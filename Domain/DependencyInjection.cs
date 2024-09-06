using Domain.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Domain;

public static class DependencyInjection {
    public static IServiceCollection AddSettings(this IServiceCollection services, IHostApplicationBuilder builder) {
        services.Configure<DatabaseSettings>(builder.Configuration.GetSection(nameof(DatabaseSettings)));
        return services;
    }
}