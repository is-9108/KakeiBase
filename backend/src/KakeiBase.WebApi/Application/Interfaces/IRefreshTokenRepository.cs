using KakeiBase.WebApi.Domain.Entities;

namespace KakeiBase.WebApi.Application.Interfaces;

/// <summary>リフレッシュトークンの永続化操作を抽象化するリポジトリインターフェース</summary>
public interface IRefreshTokenRepository
{
    /// <summary>ハッシュ値でリフレッシュトークンを検索する</summary>
    /// <returns>指定ハッシュに一致するリフレッシュトークン。存在しない場合は null</returns>
    Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>リフレッシュトークンをコンテキストに追加する</summary>
    Task AddAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>変更をデータベースに保存する</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
