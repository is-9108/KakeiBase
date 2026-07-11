namespace KakeiBase.WebApi.Endpoints;

public static class HealthEndpoint
{
    public static void MapHealthEndpoint(this WebApplication app)
    {
        app.MapHealthChecks("/health");
    }
}
