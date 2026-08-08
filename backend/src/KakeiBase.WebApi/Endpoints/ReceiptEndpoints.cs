using System.IdentityModel.Tokens.Jwt;
using KakeiBase.WebApi.Application.UseCases.Receipts;

namespace KakeiBase.WebApi.Endpoints;

public static class ReceiptEndpoints
{
    public static void MapReceiptEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/receipts").RequireAuthorization();
        group.MapPost("/presigned-url", GeneratePresignedUrl);
    }

    private static async Task<IResult> GeneratePresignedUrl(
        GeneratePresignedUrlUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        var result = await useCase.ExecuteAsync(userId.Value, ct);
        return Results.Ok(result);
    }

    /// <summary>JWT の sub クレームからユーザーIDを取得する</summary>
    /// <returns>ユーザーID。JWT に sub クレームが存在しない場合は null</returns>
    private static Guid? GetUserId(HttpContext httpContext)
    {
        var sub = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
