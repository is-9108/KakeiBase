using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TransactionType Type { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Category() { }

    public static Category Create(Guid userId, string name, TransactionType type)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Type = type,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
