using KakeiBase.WebApi.Application.DTOs.Transactions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Application.UseCases.Transactions;

public record UpdateTransactionResult(bool IsNotFound, TransactionDto? Transaction)
{
    public static UpdateTransactionResult NotFound() => new(true, null);
    public static UpdateTransactionResult Success(TransactionDto dto) => new(false, dto);
}

public class UpdateTransactionUseCase(ITransactionRepository transactionRepository)
{
    public async Task<UpdateTransactionResult> ExecuteAsync(
        Guid userId,
        Guid transactionId,
        Guid categoryId,
        int amount,
        TransactionType type,
        DateOnly date,
        string? memo,
        string? receiptS3Key,
        CancellationToken ct = default)
    {
        var transaction = await transactionRepository.FindByIdAsync(transactionId, ct);
        if (transaction is null || transaction.UserId != userId)
            return UpdateTransactionResult.NotFound();

        transaction.Update(categoryId, amount, type, date, memo, receiptS3Key);
        await transactionRepository.SaveChangesAsync(ct);

        return UpdateTransactionResult.Success(new TransactionDto(
            transaction.Id, transaction.CategoryId, transaction.SubscriptionId, transaction.Amount,
            transaction.Type, transaction.Date, transaction.Memo, transaction.ReceiptS3Key,
            transaction.CreatedAt, transaction.UpdatedAt));
    }
}
