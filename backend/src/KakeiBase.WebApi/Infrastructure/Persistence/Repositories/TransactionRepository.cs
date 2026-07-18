using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KakeiBase.WebApi.Infrastructure.Persistence.Repositories;

public class TransactionRepository(KakeiBaseDbContext dbContext) : ITransactionRepository
{
    public Task<Transaction?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<Transaction>> FindAllByUserIdAsync(
        Guid userId,
        int? year,
        int? month,
        Guid? categoryId,
        CancellationToken ct = default)
    {
        var query = dbContext.Transactions.Where(t => t.UserId == userId);

        // year フィルター適用
        if (year.HasValue)
            query = query.Where(t => t.Date.Year == year.Value);

        // month フィルター適用
        if (month.HasValue)
            query = query.Where(t => t.Date.Month == month.Value);

        // categoryId フィルター適用
        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        return query.OrderByDescending(t => t.Date).ToListAsync(ct);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
        => await dbContext.Transactions.AddAsync(transaction, ct);

    public Task DeleteAsync(Transaction transaction, CancellationToken ct = default)
    {
        dbContext.Transactions.Remove(transaction);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => dbContext.SaveChangesAsync(ct);
}
