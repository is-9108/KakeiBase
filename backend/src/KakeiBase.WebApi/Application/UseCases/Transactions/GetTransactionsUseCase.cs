using KakeiBase.WebApi.Application.DTOs.Transactions;
using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Transactions;

/// <summary>収支一覧を取得するユースケース</summary>
public class GetTransactionsUseCase(ITransactionRepository transactionRepository)
{
    /// <param name="userId">取得対象ユーザーのID</param>
    /// <param name="year">絞り込む年。null の場合は全年を対象とする</param>
    /// <param name="month">絞り込む月（1–12）。null の場合は全月を対象とする</param>
    /// <param name="categoryId">絞り込むカテゴリID。null の場合は全カテゴリを対象とする</param>
    /// <param name="ct">キャンセルトークン</param>
    public async Task<List<TransactionDto>> ExecuteAsync(
        Guid userId,
        int? year,
        int? month,
        Guid? categoryId,
        CancellationToken ct = default)
    {
        var transactions = await transactionRepository.FindAllByUserIdAsync(userId, year, month, categoryId, ct);
        return transactions
            .Select(t => new TransactionDto(
                t.Id, t.CategoryId, t.SubscriptionId, t.Amount,
                t.Date, t.Memo, t.ReceiptS3Key, t.CreatedAt, t.UpdatedAt))
            .ToList();
    }
}
