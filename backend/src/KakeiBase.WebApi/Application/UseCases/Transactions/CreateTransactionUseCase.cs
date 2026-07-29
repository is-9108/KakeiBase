using KakeiBase.WebApi.Application.DTOs.Transactions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;

namespace KakeiBase.WebApi.Application.UseCases.Transactions;

/// <summary>新しい収支を作成するユースケース</summary>
public class CreateTransactionUseCase(ITransactionRepository transactionRepository, ICategoryRepository categoryRepository)
{
    /// <param name="userId">作成するユーザーのID</param>
    /// <param name="categoryId">カテゴリのID</param>
    /// <param name="amount">金額（円単位）</param>
    /// <param name="date">収支が発生した日付</param>
    /// <param name="memo">メモ（省略可）</param>
    /// <param name="receiptS3Key">領収書画像の S3 オブジェクトキー（省略可）</param>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>作成された収支。カテゴリが存在しない・所有者が異なる・システムカテゴリの場合は null</returns>
    public async Task<TransactionDto?> ExecuteAsync(
        Guid userId,
        Guid categoryId,
        int amount,
        DateOnly date,
        string? memo,
        string? receiptS3Key,
        CancellationToken ct = default)
    {
        var category = await categoryRepository.FindByIdAsync(categoryId, ct);
        // システムカテゴリはユーザーが手動で収支に紐づけることを禁止する
        if (category is null || category.UserId != userId || category.IsSystem)
            return null;

        var transaction = Transaction.Create(userId, categoryId, amount, date, memo, receiptS3Key);
        await transactionRepository.AddAsync(transaction, ct);
        await transactionRepository.SaveChangesAsync(ct);

        return new TransactionDto(
            transaction.Id, transaction.CategoryId, category.Name, transaction.SubscriptionId,
            transaction.Amount, transaction.Date, transaction.Memo, transaction.ReceiptS3Key,
            transaction.CreatedAt, transaction.UpdatedAt);
    }
}
