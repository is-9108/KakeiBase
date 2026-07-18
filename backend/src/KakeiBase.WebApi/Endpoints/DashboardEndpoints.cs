using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using KakeiBase.WebApi.Application.UseCases.Dashboard;
using KakeiBase.WebApi.Endpoints.Requests;

namespace KakeiBase.WebApi.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dashboard/summary", GetSummary).RequireAuthorization();
    }

    private static async Task<IResult> GetSummary(
        [AsParameters] GetDashboardSummaryRequest request,
        IValidator<GetDashboardSummaryRequest> validator,
        GetDashboardSummaryUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        var summary = await useCase.ExecuteAsync(userId.Value, request.Year!.Value, request.Month!.Value, ct);
        return Results.Ok(summary);
    }

    /// <summary>JWT の sub クレームからユーザーIDを取得する</summary>
    /// <returns>ユーザーID。JWT に sub クレームが存在しない場合は null</returns>
    private static Guid? GetUserId(HttpContext httpContext)
    {
        var sub = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
