using KakeiBase.WebApi.Application.DTOs.Transactions;
using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Transactions;

public record UpdateTransactionResult(bool IsNotFound, TransactionDto? Transaction)
{
    /// <summary>対象収支が存在しない、または別ユーザーのリソースである場合の結果を生成する</summary>
    public static UpdateTransactionResult NotFound() => new(true, null);
    /// <summary>更新成功時の結果を生成する</summary>
    public static UpdateTransactionResult Success(TransactionDto dto) => new(false, dto);
}

/// <summary>収支情報を更新するユースケース</summary>
public class UpdateTransactionUseCase(ITransactionRepository transactionRepository, ICategoryRepository categoryRepository)
{
    /// <param name="userId">リクエストユーザーのID</param>
    /// <param name="transactionId">更新する収支のID</param>
    /// <param name="categoryId">新しいカテゴリのID</param>
    /// <param name="amount">新しい金額（円単位）</param>
    /// <param name="date">新しい発生日付</param>
    /// <param name="memo">新しいメモ（null で削除）</param>
    /// <param name="receiptS3Key">新しい領収書 S3 キー（null で削除）</param>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>更新結果。IsNotFound は未存在または別ユーザーのリソースを示す</returns>
    public async Task<UpdateTransactionResult> ExecuteAsync(
        Guid userId,
        Guid transactionId,
        Guid categoryId,
        int amount,
        DateOnly date,
        string? memo,
        string? receiptS3Key,
        CancellationToken ct = default)
    {
        var transaction = await transactionRepository.FindByIdAsync(transactionId, ct);
        if (transaction is null || transaction.UserId != userId)
            return UpdateTransactionResult.NotFound();

        var category = await categoryRepository.FindByIdAsync(categoryId, ct);
        if (category is null || category.UserId != userId)
            return UpdateTransactionResult.NotFound();

        transaction.Update(categoryId, amount, date, memo, receiptS3Key);
        await transactionRepository.SaveChangesAsync(ct);

        return UpdateTransactionResult.Success(new TransactionDto(
            transaction.Id, transaction.CategoryId, transaction.SubscriptionId, transaction.Amount,
            transaction.Date, transaction.Memo, transaction.ReceiptS3Key,
            transaction.CreatedAt, transaction.UpdatedAt));
    }
}
