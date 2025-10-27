using Microsoft.Extensions.DependencyInjection;

namespace Vonage_SSW_Workshop.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        return services;
    }
}