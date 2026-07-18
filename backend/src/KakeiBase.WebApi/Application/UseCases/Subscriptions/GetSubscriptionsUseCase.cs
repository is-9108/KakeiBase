using KakeiBase.WebApi.Application.DTOs.Subscriptions;
using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Subscriptions;

public class GetSubscriptionsUseCase(ISubscriptionRepository subscriptionRepository)
{
    public async Task<List<SubscriptionDto>> ExecuteAsync(
        Guid userId,
        bool? isActive,
        CancellationToken ct = default)
    {
        var subscriptions = await subscriptionRepository.FindAllByUserIdAsync(userId, isActive, ct);
        return subscriptions
            .Select(s => new SubscriptionDto(
                s.Id, s.CategoryId, s.Name, s.Amount, s.IsActive, s.CreatedAt, s.UpdatedAt))
            .ToList();
    }
}
