namespace KakeiBase.WebApi.Domain.Entities;

public class Subscription
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Amount { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Subscription() { }

    public static Subscription Create(Guid userId, Guid categoryId, string name, int amount)
    {
        var now = DateTimeOffset.UtcNow;
        return new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = categoryId,
            Name = name,
            Amount = amount,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public Transaction GenerateTransaction(DateOnly date)
    {
        if (!IsActive)
            throw new InvalidOperationException(
                $"サブスク '{Name}' は無効のため、収支レコードを生成できません。");

        return Transaction.FromSubscription(this, date);
    }

    public void Update(Guid categoryId, string name, int amount, bool isActive)
    {
        CategoryId = categoryId;
        Name = name;
        Amount = amount;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
