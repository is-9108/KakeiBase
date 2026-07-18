using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Subscriptions;

public class DeleteSubscriptionUseCase(ISubscriptionRepository subscriptionRepository)
{
    public async Task<bool> ExecuteAsync(Guid userId, Guid subscriptionId, CancellationToken ct = default)
    {
        var subscription = await subscriptionRepository.FindByIdAsync(subscriptionId, ct);
        if (subscription is null || subscription.UserId != userId)
            return false;

        await subscriptionRepository.DeleteAsync(subscription, ct);
        await subscriptionRepository.SaveChangesAsync(ct);

        return true;
    }
}
