using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid? SubscriptionId { get; private set; }
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Memo { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Transaction() { }

    public static Transaction Create(
        Guid userId,
        Guid categoryId,
        decimal amount,
        TransactionType type,
        DateOnly date,
        string? memo = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = categoryId,
            Amount = amount,
            Type = type,
            Date = date,
            Memo = memo,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    internal static Transaction FromSubscription(Subscription subscription, DateOnly date)
    {
        var now = DateTimeOffset.UtcNow;
        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = subscription.UserId,
            CategoryId = subscription.CategoryId,
            SubscriptionId = subscription.Id,
            Amount = subscription.Amount,
            Type = TransactionType.Expense,
            Date = date,
            Memo = subscription.Name,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
