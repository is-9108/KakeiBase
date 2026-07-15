using KakeiBase.WebApi.Domain.Entities;

namespace KakeiBase.WebApi.Application.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Transaction>> FindAllByUserIdAsync(Guid userId, int? year, int? month, Guid? categoryId, CancellationToken ct = default);
    Task AddAsync(Transaction transaction, CancellationToken ct = default);
    Task DeleteAsync(Transaction transaction, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
