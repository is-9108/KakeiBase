using KakeiBase.WebApi.Application.DTOs.Transactions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Application.UseCases.Transactions;

public class CreateTransactionUseCase(ITransactionRepository transactionRepository)
{
    public async Task<TransactionDto> ExecuteAsync(
        Guid userId,
        Guid categoryId,
        decimal amount,
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
