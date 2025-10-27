namespace Vonage_SSW_Workshop.WebApi;

public static class DependencyInjection
{
    public static void AddWebApi(this IServiceCollection services)
    {
        services.AddOpenApi();
    }
}