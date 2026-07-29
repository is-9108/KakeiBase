using KakeiBase.WebApi.Application.DTOs.Subscriptions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;

namespace KakeiBase.WebApi.Application.UseCases.Subscriptions;

public class CreateSubscriptionUseCase(ISubscriptionRepository subscriptionRepository, ICategoryRepository categoryRepository)
{
    /// <returns>作成されたサブスクリプション。システムカテゴリが未設定の場合は null</returns>
    public async Task<SubscriptionDto?> ExecuteAsync(
        Guid userId,
        string name,
        int amount,
        CancellationToken ct = default)
    {
        var systemCategory = await categoryRepository.FindSystemSubscriptionCategoryByUserIdAsync(userId, ct);
        if (systemCategory is null)
            return null;

        var subscription = Subscription.Create(userId, systemCategory.Id, name, amount);
        await subscriptionRepository.AddAsync(subscription, ct);
        await subscriptionRepository.SaveChangesAsync(ct);

        return new SubscriptionDto(
            subscription.Id, subscription.CategoryId, subscription.Name,
            subscription.Amount, subscription.IsActive,
            subscription.CreatedAt, subscription.UpdatedAt);
    }
}
