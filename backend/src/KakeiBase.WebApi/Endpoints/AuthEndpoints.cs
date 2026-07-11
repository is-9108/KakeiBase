using FluentValidation;
using KakeiBase.WebApi.Application.DTOs.Auth;
using KakeiBase.WebApi.Application.UseCases.Auth;
using KakeiBase.WebApi.Endpoints.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KakeiBase.WebApi.Endpoints;

public static class AuthEndpoints
{
    private const string RefreshTokenCookieName = "refresh_token";
    private const string AccessTokenCookieName = "access_token";

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", Login);
        group.MapPost("/refresh", Refresh);
        group.MapPost("/logout", Logout);
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        IValidator<LoginRequest> validator,
        LoginUseCase loginUseCase,
        HttpContext httpContext,
        IHostEnvironment env,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var result = await loginUseCase.ExecuteAsync(request.Email, request.Password, ct);
        if (result is null)
            return Results.Unauthorized();

        SetTokenCookies(httpContext, result, env.IsDevelopment());

        return Results.Ok(new AuthResponse(result.AccessTokenExpiresAt));
    }

    private static async Task<IResult> Refresh(
        RefreshTokenUseCase refreshTokenUseCase,
        HttpContext httpContext,
        IHostEnvironment env,
        CancellationToken ct)
    {
        var rawToken = httpContext.Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrEmpty(rawToken))
            return Results.Unauthorized();

        var result = await refreshTokenUseCase.ExecuteAsync(rawToken, ct);
        if (result is null)
            return Results.Unauthorized();

        SetTokenCookies(httpContext, result, env.IsDevelopment());

        return Results.Ok(new AuthResponse(result.AccessTokenExpiresAt));
    }

    private static async Task<IResult> Logout(
        LogoutUseCase logoutUseCase,
        HttpContext httpContext,
        IHostEnvironment env,
        CancellationToken ct)
    {
        var rawToken = httpContext.Request.Cookies[RefreshTokenCookieName];
        if (!string.IsNullOrEmpty(rawToken))
            await logoutUseCase.ExecuteAsync(rawToken, ct);

        var isSecure = !env.IsDevelopment();
        httpContext.Response.Cookies.Delete(AccessTokenCookieName, new CookieOptions { Secure = isSecure });
        httpContext.Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Secure = isSecure });

        return Results.Ok();
    }

    private static void SetTokenCookies(HttpContext httpContext, LoginResult result, bool isDevelopment)
    {
        var isSecure = !isDevelopment;

        httpContext.Response.Cookies.Append(AccessTokenCookieName, result.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = isSecure,
            MaxAge = TimeSpan.FromMinutes(15)
        });

        httpContext.Response.Cookies.Append(RefreshTokenCookieName, result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = isSecure,
            MaxAge = TimeSpan.FromDays(7)
        });
    }
}
