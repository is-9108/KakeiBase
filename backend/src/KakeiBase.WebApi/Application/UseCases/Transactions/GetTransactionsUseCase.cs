using KakeiBase.WebApi.Application.DTOs.Transactions;
using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Transactions;

public class GetTransactionsUseCase(ITransactionRepository transactionRepository)
{
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
                t.Id, t.CategoryId, t.SubscriptionId, t.Amount, t.Type,
                t.Date, t.Memo, t.ReceiptS3Key, t.CreatedAt, t.UpdatedAt))
            .ToList();
    }
}
