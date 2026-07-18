using KakeiBase.WebApi.Application.DTOs.Subscriptions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;

namespace KakeiBase.WebApi.Application.UseCases.Subscriptions;

public class CreateSubscriptionUseCase(ISubscriptionRepository subscriptionRepository)
{
    public async Task<SubscriptionDto> ExecuteAsync(
        Guid userId,
        Guid categoryId,
        string name,
        int amount,
        CancellationToken ct = default)
    {
        var subscription = Subscription.Create(userId, categoryId, name, amount);
        await subscriptionRepository.AddAsync(subscription, ct);
        await subscriptionRepository.SaveChangesAsync(ct);

        return new SubscriptionDto(
            subscription.Id, subscription.CategoryId, subscription.Name,
            subscription.Amount, subscription.IsActive,
            subscription.CreatedAt, subscription.UpdatedAt);
    }
}
