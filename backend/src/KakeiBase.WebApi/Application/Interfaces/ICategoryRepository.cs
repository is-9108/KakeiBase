using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Application.Interfaces;

/// <summary>カテゴリの永続化操作を抽象化するリポジトリインターフェース</summary>
public interface ICategoryRepository
{
    /// <summary>指定 ID のカテゴリを取得する</summary>
    /// <returns>指定 ID のカテゴリ。存在しない場合は null</returns>
    Task<Category?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>指定ユーザーが所有するカテゴリを全件取得する</summary>
    /// <param name="userId">取得対象ユーザーのID</param>
    Task<List<Category>> FindAllByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>同一ユーザー・同一名前・同一区分のカテゴリが存在するか確認する</summary>
    /// <param name="userId">確認対象ユーザーのID</param>
    /// <param name="name">カテゴリ名</param>
    /// <param name="type">収支区分</param>
    /// <param name="excludeId">重複チェックから除外するカテゴリID（更新時に自身を除くために使用）</param>
    /// <returns>重複するカテゴリが存在する場合は true</returns>
    Task<bool> ExistsByUserIdAndNameAndTypeAsync(Guid userId, string name, TransactionType type, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>カテゴリをコンテキストに追加する</summary>
    Task AddAsync(Category category, CancellationToken ct = default);

    /// <summary>カテゴリをコンテキストから削除する</summary>
    Task DeleteAsync(Category category, CancellationToken ct = default);

    /// <summary>変更をデータベースに保存する</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
