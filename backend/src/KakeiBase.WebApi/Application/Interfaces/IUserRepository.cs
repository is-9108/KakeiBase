using KakeiBase.WebApi.Domain.Entities;

namespace KakeiBase.WebApi.Application.Interfaces;

/// <summary>ユーザーの永続化操作を抽象化するリポジトリインターフェース</summary>
public interface IUserRepository
{
    /// <summary>メールアドレスでユーザーを検索する</summary>
    /// <returns>指定メールアドレスのユーザー。存在しない場合は null</returns>
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>指定 ID のユーザーを取得する</summary>
    /// <returns>指定 ID のユーザー。存在しない場合は null</returns>
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);
}
