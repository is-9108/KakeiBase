using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Domain.Entities;

/// <summary>収支の1件を表すエンティティ</summary>
public class Transaction
{
    /// <summary>ID</summary>
    public Guid Id { get; private set; }
    /// <summary>ユーザーID</summary>
    public Guid UserId { get; private set; }
    /// <summary>収支が属するカテゴリのID</summary>
    public Guid CategoryId { get; private set; }
    /// <summary>定期支出から自動生成された場合の元サブスクリプションID。手動登録の場合は null</summary>
    public Guid? SubscriptionId { get; private set; }
    /// <summary>金額（円単位）</summary>
    public int Amount { get; private set; }
    /// <summary>収入または支出の区分</summary>
    public TransactionType Type { get; private set; }
    public DateOnly Date { get; private set; }
    /// <summary>メモ</summary>
    public string? Memo { get; private set; }
    /// <summary>領収書画像の S3 オブジェクトキー。未添付の場合は null</summary>
    public string? ReceiptS3Key { get; private set; }
    /// <summary>レコード作成日時（UTC）</summary>
    public DateTimeOffset CreatedAt { get; private set; }
    /// <summary>レコード最終更新日時（UTC）</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    private Transaction() { }

    /// <summary>新しい収支レコードを作成する</summary>
    /// <param name="userId">所有ユーザーのID</param>
    /// <param name="categoryId">カテゴリのID</param>
    /// <param name="amount">金額（円単位）</param>
    /// <param name="type">収入または支出の区分</param>
    /// <param name="date">収支が発生した日付</param>
    /// <param name="memo">メモ（省略可）</param>
    /// <param name="receiptS3Key">領収書画像の S3 オブジェクトキー（省略可）</param>
    /// <returns>作成された収支エンティティ</returns>
    public static Transaction Create(
        Guid userId,
        Guid categoryId,
        int amount,
        TransactionType type,
        DateOnly date,
        string? memo = null,
        string? receiptS3Key = null)
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
            ReceiptS3Key = receiptS3Key,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>収支情報を更新する</summary>
    /// <param name="categoryId">新しいカテゴリのID</param>
    /// <param name="amount">新しい金額（円単位）</param>
    /// <param name="type">新しい収支区分</param>
    /// <param name="date">新しい発生日付</param>
    /// <param name="memo">新しいメモ（null で削除）</param>
    /// <param name="receiptS3Key">新しい領収書 S3 キー（null で削除）</param>
    public void Update(
        Guid categoryId,
        int amount,
        TransactionType type,
        DateOnly date,
        string? memo,
        string? receiptS3Key)
    {
        CategoryId = categoryId;
        Amount = amount;
        Type = type;
        Date = date;
        Memo = memo;
        ReceiptS3Key = receiptS3Key;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>定期支出エンティティから収支レコードを生成する内部ファクトリ</summary>
    /// <param name="subscription">元となる定期支出</param>
    /// <param name="date">収支を発生させる日付</param>
    /// <returns>定期支出に紐づく収支エンティティ</returns>
    internal static Transaction FromSubscription(Subscription subscription, DateOnly date)
    {
        var now = DateTimeOffset.UtcNow;
        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = subscription.UserId,
            CategoryId = subscription.CategoryId,
            SubscriptionId = subscription.Id,
            Amount = (int)subscription.Amount,
            Type = TransactionType.Expense,
            Date = date,
            Memo = subscription.Name,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
