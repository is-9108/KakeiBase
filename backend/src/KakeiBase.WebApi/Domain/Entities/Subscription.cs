namespace KakeiBase.WebApi.Domain.Entities;

/// <summary>定期支出（サブスクリプション）を表すエンティティ</summary>
public class Subscription
{
    /// <summary>サブスクリプションID</summary>
    public Guid Id { get; private set; }
    /// <summary>ユーザーID</summary>
    public Guid UserId { get; private set; }
    /// <summary>定期支出が属するカテゴリのID</summary>
    public Guid CategoryId { get; private set; }
    /// <summary>サブスクリプション名</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>毎回発生する金額（円単位）</summary>
    public int Amount { get; private set; }
    /// <summary>定期支出が有効かどうかを示すフラグ。false の場合は収支を生成しない</summary>
    public bool IsActive { get; private set; }
    /// <summary>レコード作成日時（UTC）</summary>
    public DateTimeOffset CreatedAt { get; private set; }
    /// <summary>レコード最終更新日時（UTC）</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    private Subscription() { }

    /// <summary>新しい定期支出を作成する</summary>
    /// <param name="userId">所有ユーザーのID</param>
    /// <param name="categoryId">カテゴリのID</param>
    /// <param name="name">定期支出の名称</param>
    /// <param name="amount">金額（円単位）</param>
    /// <returns>作成された定期支出エンティティ（IsActive = true）</returns>
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

    /// <summary>指定した日付で収支レコードを生成する</summary>
    /// <param name="date">収支を発生させる日付</param>
    /// <returns>この定期支出に紐づく収支エンティティ</returns>
    /// <exception cref="InvalidOperationException">IsActive が false の場合にスロー</exception>
    public Transaction GenerateTransaction(DateOnly date)
    {
        if (!IsActive)
            throw new InvalidOperationException(
                $"サブスク '{Name}' は無効のため、収支レコードを生成できません。");

        return Transaction.FromSubscription(this, date);
    }

    /// <summary>定期支出を無効化する</summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
