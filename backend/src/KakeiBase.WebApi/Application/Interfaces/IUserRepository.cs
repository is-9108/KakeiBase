using KakeiBase.WebApi.Domain.Entities;

namespace KakeiBase.WebApi.Application.Interfaces;

/// <summary>ユーザーの永続化操作を抽象化するリポジトリインターフェース</summary>
public interface IUserRepository
{
    /// <returns>指定メールアドレスのユーザー。存在しない場合は null</returns>
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);

    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);
}
