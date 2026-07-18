using KakeiBase.WebApi.Application.DTOs.Categories;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Application.UseCases.Categories;

public record UpdateCategoryResult(CategoryDto? Category, bool IsConflict)
{
    /// <summary>対象カテゴリが存在しない、または別ユーザーのリソースである場合の結果を生成する</summary>
    public static UpdateCategoryResult NotFound() => new(null, false);
    /// <summary>同名・同種別のカテゴリが既に存在する場合の結果を生成する</summary>
    public static UpdateCategoryResult Conflict() => new(null, true);
    /// <summary>更新成功時の結果を生成する</summary>
    public static UpdateCategoryResult Success(CategoryDto dto) => new(dto, false);
}

/// <summary>カテゴリ情報を更新するユースケース</summary>
public class UpdateCategoryUseCase(ICategoryRepository categoryRepository)
{
    /// <param name="userId">リクエストユーザーのID</param>
    /// <param name="categoryId">更新するカテゴリのID</param>
    /// <param name="name">新しいカテゴリ名</param>
    /// <param name="type">新しい収支区分</param>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>更新結果。IsNotFound は未存在または別ユーザー、IsConflict は名前・種別の重複を示す</returns>
    public async Task<UpdateCategoryResult> ExecuteAsync(Guid userId, Guid categoryId, string name, TransactionType type, CancellationToken ct = default)
    {
        var category = await categoryRepository.FindByIdAsync(categoryId, ct);
        if (category is null || category.UserId != userId)
            return UpdateCategoryResult.NotFound();

        var exists = await categoryRepository.ExistsByUserIdAndNameAndTypeAsync(userId, name, type, excludeId: categoryId, ct: ct);
        if (exists)
            return UpdateCategoryResult.Conflict();

        category.Update(name, type);
        await categoryRepository.SaveChangesAsync(ct);

        return UpdateCategoryResult.Success(new CategoryDto(category.Id, category.Name, category.Type, category.CreatedAt));
    }
}
