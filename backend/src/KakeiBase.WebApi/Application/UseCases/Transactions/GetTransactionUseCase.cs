using KakeiBase.WebApi.Application.DTOs.Transactions;
using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Transactions;

public class GetTransactionUseCase(ITransactionRepository transactionRepository)
{
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
