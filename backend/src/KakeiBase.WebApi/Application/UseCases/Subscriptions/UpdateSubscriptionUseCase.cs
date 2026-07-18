using KakeiBase.WebApi.Application.DTOs.Subscriptions;
using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Subscriptions;

public record UpdateSubscriptionResult(bool IsNotFound, SubscriptionDto? Subscription)
{
    public static UpdateSubscriptionResult NotFound() => new(true, null);
    public static UpdateSubscriptionResult Success(SubscriptionDto dto) => new(false, dto);
}

public class UpdateSubscriptionUseCase(ISubscriptionRepository subscriptionRepository)
{
    public async Task<UpdateSubscriptionResult> ExecuteAsync(
        Guid userId,
        Guid subscriptionId,
        Guid categoryId,
        string name,
        int amount,
        bool isActive,
        CancellationToken ct = default)
    {
        var subscription = await subscriptionRepository.FindByIdAsync(subscriptionId, ct);
        if (subscription is null || subscription.UserId != userId)
            return UpdateSubscriptionResult.NotFound();

        subscription.Update(categoryId, name, amount, isActive);
        await subscriptionRepository.SaveChangesAsync(ct);

        return UpdateSubscriptionResult.Success(new SubscriptionDto(
            subscription.Id, subscription.CategoryId, subscription.Name,
            subscription.Amount, subscription.IsActive,
            subscription.CreatedAt, subscription.UpdatedAt));
    }
}
