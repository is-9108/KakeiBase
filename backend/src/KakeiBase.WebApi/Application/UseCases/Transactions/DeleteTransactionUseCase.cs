using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Transactions;

public class DeleteTransactionUseCase(ITransactionRepository transactionRepository)
{
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
