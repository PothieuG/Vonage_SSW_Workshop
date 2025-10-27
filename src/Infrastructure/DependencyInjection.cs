using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Vonage_SSW_Workshop.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddSingleton(TimeProvider.System);
    }
}