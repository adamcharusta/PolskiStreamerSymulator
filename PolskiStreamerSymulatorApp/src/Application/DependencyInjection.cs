using Microsoft.Extensions.DependencyInjection;

namespace PolskiStreamerSymulatorApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
