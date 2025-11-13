using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vonage_SSW_Workshop.Infrastructure.Vonage;
using Vonage_SSW_Workshop.Application.Common.Interfaces;
using Vonage.Extensions;

namespace Vonage_SSW_Workshop.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        builder.Services.AddVonageClientScoped(builder.Configuration);
        builder.Services.Configure<WorkshopSettings>( builder.Configuration.GetSection(WorkshopSettings.SectionName));
        services.AddScoped<IVonageService, VonageService>();
        services.AddSingleton(TimeProvider.System);
    }
}