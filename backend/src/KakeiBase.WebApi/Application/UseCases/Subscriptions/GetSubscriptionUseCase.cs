using KakeiBase.WebApi.Application.DTOs.Subscriptions;
using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Subscriptions;

public class GetSubscriptionUseCase(ISubscriptionRepository subscriptionRepository)
{
    public async Task<SubscriptionDto?> ExecuteAsync(Guid userId, Guid subscriptionId, CancellationToken ct = default)
    {
        var subscription = await subscriptionRepository.FindByIdAsync(subscriptionId, ct);
        if (subscription is null || subscription.UserId != userId)
            return null;

        return new SubscriptionDto(
            subscription.Id, subscription.CategoryId, subscription.Name,
            subscription.Amount, subscription.IsActive,
            subscription.CreatedAt, subscription.UpdatedAt);
    }
}
