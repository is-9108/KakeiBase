using KakeiBase.WebApi.Application.DTOs.Transactions;
using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Transactions;

/// <summary>収支を1件取得するユースケース</summary>
public class GetTransactionUseCase(ITransactionRepository transactionRepository)
{
    /// <param name="userId">リクエストユーザーのID</param>
    /// <param name="transactionId">取得する収支のID</param>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>収支が存在する場合はDTO。未存在または別ユーザーのリソースの場合は null</returns>
    public async Task<TransactionDto?> ExecuteAsync(Guid userId, Guid transactionId, CancellationToken ct = default)
    {
        var transaction = await transactionRepository.FindByIdAsync(transactionId, ct);
        if (transaction is null || transaction.UserId != userId)
            return null;

        return new TransactionDto(
            transaction.Id, transaction.CategoryId, transaction.SubscriptionId, transaction.Amount,
            transaction.Type, transaction.Date, transaction.Memo, transaction.ReceiptS3Key,
            transaction.CreatedAt, transaction.UpdatedAt);
    }
}
