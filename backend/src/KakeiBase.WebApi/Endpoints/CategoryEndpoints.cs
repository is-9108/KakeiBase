using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using KakeiBase.WebApi.Application.UseCases.Categories;
using KakeiBase.WebApi.Endpoints.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KakeiBase.WebApi.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/categories").RequireAuthorization();

        group.MapGet("/", GetCategories);
        group.MapGet("/{id:guid}", GetCategory);
        group.MapPost("/", CreateCategory);
        group.MapPut("/{id:guid}", UpdateCategory);
        group.MapDelete("/{id:guid}", DeleteCategory);
    }

    private static async Task<IResult> GetCategories(
        GetCategoriesUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        var categories = await useCase.ExecuteAsync(userId.Value, ct);
        return Results.Ok(categories);
    }

    private static async Task<IResult> GetCategory(
        Guid id,
        GetCategoryUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        var category = await useCase.ExecuteAsync(userId.Value, id, ct);
        return category is null ? Results.NotFound() : Results.Ok(category);
    }

    private static async Task<IResult> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        IValidator<CreateCategoryRequest> validator,
        CreateCategoryUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        var result = await useCase.ExecuteAsync(userId.Value, request.Name, request.Type, ct);
        return result is null
            ? Results.Conflict()
            : Results.Created($"/api/categories/{result.Id}", result);
    }

    private static async Task<IResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        IValidator<UpdateCategoryRequest> validator,
        UpdateCategoryUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        var result = await useCase.ExecuteAsync(userId.Value, id, request.Name, request.Type, ct);

        if (result.IsConflict)
            return Results.Conflict();
        if (result.Category is null)
            return Results.NotFound();

        return Results.Ok(result.Category);
    }

    private static async Task<IResult> DeleteCategory(
        Guid id,
        DeleteCategoryUseCase useCase,
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
