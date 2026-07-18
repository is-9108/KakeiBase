using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Transactions;

/// <summary>収支を削除するユースケース</summary>
public class DeleteTransactionUseCase(ITransactionRepository transactionRepository)
{
    /// <param name="userId">リクエストユーザーのID</param>
    /// <param name="transactionId">削除する収支のID</param>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>削除に成功した場合は true。未存在または別ユーザーのリソースの場合は false</returns>
    public async Task<bool> ExecuteAsync(Guid userId, Guid transactionId, CancellationToken ct = default)
    {
        var transaction = await transactionRepository.FindByIdAsync(transactionId, ct);
        if (transaction is null || transaction.UserId != userId)
            return false;

        await transactionRepository.DeleteAsync(transaction, ct);
        await transactionRepository.SaveChangesAsync(ct);

        return true;
    }
}
