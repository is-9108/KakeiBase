namespace KakeiBase.WebApi.Application.DTOs.Transactions;

public record TransactionDto(
    Guid Id,
    Guid CategoryId,
    Guid? SubscriptionId,
    int Amount,
    DateOnly Date,
    string? Memo,
    string? ReceiptS3Key,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
