using KakeiBase.WebApi.Domain.Entities;

namespace KakeiBase.WebApi.Application.Interfaces;

/// <summary>収支の永続化操作を抽象化するリポジトリインターフェース</summary>
public interface ITransactionRepository
{
    /// <summary>指定 ID の収支を取得する</summary>
    /// <returns>指定 ID の収支。存在しない場合は null</returns>
    Task<Transaction?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>指定ユーザーの収支一覧を取得する</summary>
    /// <param name="userId">取得対象ユーザーのID</param>
    /// <param name="year">絞り込む年。null の場合は全年を対象とする</param>
    /// <param name="month">絞り込む月（1–12）。null の場合は全月を対象とする</param>
    /// <param name="categoryId">絞り込むカテゴリID。null の場合は全カテゴリを対象とする</param>
    /// <param name="ct">キャンセルトークン</param>
    Task<List<Transaction>> FindAllByUserIdAsync(Guid userId, int? year, int? month, Guid? categoryId, CancellationToken ct = default);

    /// <summary>収支をコンテキストに追加する</summary>
    Task AddAsync(Transaction transaction, CancellationToken ct = default);

    /// <summary>収支をコンテキストから削除する</summary>
    Task DeleteAsync(Transaction transaction, CancellationToken ct = default);

    /// <summary>変更をデータベースに保存する</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
