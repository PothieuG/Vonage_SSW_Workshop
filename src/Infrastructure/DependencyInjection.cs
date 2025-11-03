using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vonage_SSW_Workshop.Infrastructure.Vonage;
using Vonage_SSW_Workshop.Application.Common.Interfaces;

namespace Vonage_SSW_Workshop.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        builder.Services.Configure<VonageSettings>(
            builder.Configuration.GetSection(VonageSettings.SectionName));

        services.AddScoped<IVonageService, VonageService>();

        services.AddSingleton(TimeProvider.System);
    }
}