using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Domain.Entities;

/// <summary>収支カテゴリを表すエンティティ</summary>
public class Category
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    /// <summary>このカテゴリが収入用か支出用かを示す区分</summary>
    public TransactionType Type { get; private set; }
    /// <summary>レコード作成日時（UTC）</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    private Category() { }

    /// <summary>新しいカテゴリを作成する</summary>
    /// <param name="userId">所有ユーザーのID</param>
    /// <param name="name">カテゴリ名</param>
    /// <param name="type">収入または支出の区分</param>
    /// <returns>作成されたカテゴリエンティティ</returns>
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

    /// <summary>カテゴリ情報を更新する</summary>
    /// <param name="name">新しいカテゴリ名</param>
    /// <param name="type">新しい収支区分</param>
    public void Update(string name, TransactionType type)
    {
        Name = name;
        Type = type;
    }
}
