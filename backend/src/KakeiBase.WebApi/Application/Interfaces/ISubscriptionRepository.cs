using KakeiBase.WebApi.Domain.Entities;

namespace KakeiBase.WebApi.Application.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> FindByIdAsync(Guid id, CancellationToken ct = default);
    /// <summary>指定ユーザーのサブスクリプション一覧を取得する</summary>
    /// <param name="isActive">trueで有効のみ、falseで無効のみ、nullで全件</param>
    Task<List<Subscription>> FindAllByUserIdAsync(Guid userId, bool? isActive = null, CancellationToken ct = default);
    Task AddAsync(Subscription subscription, CancellationToken ct = default);
    Task DeleteAsync(Subscription subscription, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
