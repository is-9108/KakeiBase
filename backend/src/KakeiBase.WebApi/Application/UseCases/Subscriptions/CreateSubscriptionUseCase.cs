using KakeiBase.WebApi.Application.DTOs.Subscriptions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;

namespace KakeiBase.WebApi.Application.UseCases.Subscriptions;

public class CreateSubscriptionUseCase(ISubscriptionRepository subscriptionRepository, ICategoryRepository categoryRepository)
{
    /// <returns>作成されたサブスクリプション。カテゴリが存在しないまたは所有者が異なる場合は null</returns>
    public async Task<SubscriptionDto?> ExecuteAsync(
        Guid userId,
        Guid categoryId,
        string name,
        int amount,
        CancellationToken ct = default)
    {
        var category = await categoryRepository.FindByIdAsync(categoryId, ct);
        if (category is null || category.UserId != userId)
            return null;

        var subscription = Subscription.Create(userId, categoryId, name, amount);
        await subscriptionRepository.AddAsync(subscription, ct);
        await subscriptionRepository.SaveChangesAsync(ct);

        return new SubscriptionDto(
            subscription.Id, subscription.CategoryId, subscription.Name,
            subscription.Amount, subscription.IsActive,
            subscription.CreatedAt, subscription.UpdatedAt);
    }
}
