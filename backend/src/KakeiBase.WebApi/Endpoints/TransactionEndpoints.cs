using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using KakeiBase.WebApi.Application.UseCases.Transactions;
using KakeiBase.WebApi.Endpoints.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KakeiBase.WebApi.Endpoints;

public static class TransactionEndpoints
{
    public static void MapTransactionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/transactions").RequireAuthorization();

        group.MapGet("/", GetTransactions);
        group.MapGet("/{id:guid}", GetTransaction);
        group.MapPost("/", CreateTransaction);
        group.MapPut("/{id:guid}", UpdateTransaction);
        group.MapDelete("/{id:guid}", DeleteTransaction);
    }

    private static async Task<IResult> GetTransactions(
        GetTransactionsUseCase useCase,
        HttpContext httpContext,
        int? year,
        int? month,
        Guid? categoryId,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        var transactions = await useCase.ExecuteAsync(userId.Value, year, month, categoryId, ct);
        return Results.Ok(transactions);
    }

    private static async Task<IResult> GetTransaction(
        Guid id,
        GetTransactionUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        var transaction = await useCase.ExecuteAsync(userId.Value, id, ct);
        return transaction is null ? Results.NotFound() : Results.Ok(transaction);
    }

    private static async Task<IResult> CreateTransaction(
        [FromBody] CreateTransactionRequest request,
        IValidator<CreateTransactionRequest> validator,
        CreateTransactionUseCase useCase,
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
            userId.Value, request.CategoryId, request.Amount, request.Type,
            request.Date, request.Memo, request.ReceiptS3Key, ct);

        return result is null
            ? Results.NotFound()
            : Results.Created($"/api/transactions/{result.Id}", result);
    }

    private static async Task<IResult> UpdateTransaction(
        Guid id,
        [FromBody] UpdateTransactionRequest request,
        IValidator<UpdateTransactionRequest> validator,
        UpdateTransactionUseCase useCase,
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
            userId.Value, id, request.CategoryId, request.Amount, request.Type,
            request.Date, request.Memo, request.ReceiptS3Key, ct);

        return result.IsNotFound ? Results.NotFound() : Results.Ok(result.Transaction);
    }

    private static async Task<IResult> DeleteTransaction(
        Guid id,
        DeleteTransactionUseCase useCase,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Unauthorized();

        var deleted = await useCase.ExecuteAsync(userId.Value, id, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    /// <summary>JWT の sub クレームからユーザーIDを取得する</summary>
    /// <returns>ユーザーID。JWT に sub クレームが存在しない場合は null</returns>
    private static Guid? GetUserId(HttpContext httpContext)
    {
        var sub = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
