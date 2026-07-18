using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using KakeiBase.WebApi.Application.UseCases.Subscriptions;
using KakeiBase.WebApi.Endpoints.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KakeiBase.WebApi.Endpoints;

public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/subscriptions").RequireAuthorization();

        group.MapGet("/", GetSubscriptions);
        group.MapGet("/{id:guid}", GetSubscription);
        group.MapPost("/", CreateSubscription);
        group.MapPut("/{id:guid}", UpdateSubscription);
        group.MapDelete("/{id:guid}", DeleteSubscription);
    }

    private static async Task<IResult> GetSubscriptions(
        GetSubscriptionsUseCase useCase,
        HttpContext httpContext,
        bool? isActive,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        var subscriptions = await useCase.ExecuteAsync(userId.Value, isActive, ct);
        return Results.Ok(subscriptions);
    }

    private static async Task<IResult> GetSubscription(
        Guid id,
        GetSubscriptionUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        var subscription = await useCase.ExecuteAsync(userId.Value, id, ct);
        return subscription is null ? Results.NotFound() : Results.Ok(subscription);
    }

    private static async Task<IResult> CreateSubscription(
        [FromBody] CreateSubscriptionRequest request,
        IValidator<CreateSubscriptionRequest> validator,
        CreateSubscriptionUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        var result = await useCase.ExecuteAsync(userId.Value, request.CategoryId, request.Name, request.Amount, ct);
        return Results.Created($"/api/subscriptions/{result.Id}", result);
    }

    private static async Task<IResult> UpdateSubscription(
        Guid id,
        [FromBody] UpdateSubscriptionRequest request,
        IValidator<UpdateSubscriptionRequest> validator,
        UpdateSubscriptionUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        var result = await useCase.ExecuteAsync(
            userId.Value, id, request.CategoryId, request.Name, request.Amount, request.IsActive, ct);

        return result.IsNotFound ? Results.NotFound() : Results.Ok(result.Subscription);
    }

    private static async Task<IResult> DeleteSubscription(
        Guid id,
        DeleteSubscriptionUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        var deleted = await useCase.ExecuteAsync(userId.Value, id, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static Guid? GetUserId(HttpContext httpContext)
    {
        var sub = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
