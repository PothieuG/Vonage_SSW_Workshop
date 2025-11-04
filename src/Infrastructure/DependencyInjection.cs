using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vonage;
using Vonage.Request;
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

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<VonageSettings>>().Value;
            var privateKey = GetPrivateKeyContent(settings.ApplicationKey);
            var credentials = Credentials.FromAppIdAndPrivateKey(
                settings.ApplicationId,
                privateKey);
            return new VonageClient(credentials);
        });

        services.AddScoped<IVonageAuthenticatedHttpClient, VonageAuthenticatedHttpClient>();
        services.AddScoped<IVonageService, VonageService>();

        services.AddSingleton(TimeProvider.System);
    }

    private static string GetPrivateKeyContent(string applicationKey)
    {
        if (File.Exists(applicationKey))
            return File.ReadAllText(applicationKey);
        return applicationKey;
    }
}