using KakeiBase.WebApi.Application.DTOs.Transactions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Application.UseCases.Transactions;

/// <summary>新しい収支を作成するユースケース</summary>
public class CreateTransactionUseCase(ITransactionRepository transactionRepository)
{
    /// <param name="userId">作成するユーザーのID</param>
    /// <param name="categoryId">カテゴリのID</param>
    /// <param name="amount">金額（円単位）</param>
    /// <param name="type">収入または支出の区分</param>
    /// <param name="date">収支が発生した日付</param>
    /// <param name="memo">メモ（省略可）</param>
    /// <param name="receiptS3Key">領収書画像の S3 オブジェクトキー（省略可）</param>
    /// <param name="ct">キャンセルトークン</param>
    public async Task<TransactionDto> ExecuteAsync(
        Guid userId,
        Guid categoryId,
        int amount,
        TransactionType type,
        DateOnly date,
        string? memo,
        string? receiptS3Key,
        CancellationToken ct = default)
    {
        var transaction = Transaction.Create(userId, categoryId, amount, type, date, memo, receiptS3Key);
        await transactionRepository.AddAsync(transaction, ct);
        await transactionRepository.SaveChangesAsync(ct);

        return new TransactionDto(
            transaction.Id, transaction.CategoryId, transaction.SubscriptionId, transaction.Amount,
            transaction.Type, transaction.Date, transaction.Memo, transaction.ReceiptS3Key,
            transaction.CreatedAt, transaction.UpdatedAt);
    }
}
