using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KakeiBase.WebApi.Infrastructure.Persistence.Repositories;

public class SubscriptionRepository(KakeiBaseDbContext dbContext) : ISubscriptionRepository
{
    public Task<Subscription?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => dbContext.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<List<Subscription>> FindAllByUserIdAsync(Guid userId, bool? isActive = null, CancellationToken ct = default)
    {
        var query = dbContext.Subscriptions.Where(s => s.UserId == userId);

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        return query.OrderBy(s => s.Name).ToListAsync(ct);
    }

    public async Task AddAsync(Subscription subscription, CancellationToken ct = default)
        => await dbContext.Subscriptions.AddAsync(subscription, ct);

    public Task DeleteAsync(Subscription subscription, CancellationToken ct = default)
    {
        dbContext.Subscriptions.Remove(subscription);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => dbContext.SaveChangesAsync(ct);
}
