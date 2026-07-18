using KakeiBase.WebApi.Application.DTOs.Categories;
using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Categories;

/// <summary>カテゴリを1件取得するユースケース</summary>
public class GetCategoryUseCase(ICategoryRepository categoryRepository)
{
    /// <param name="userId">リクエストユーザーのID</param>
    /// <param name="categoryId">取得するカテゴリのID</param>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>カテゴリが存在する場合はDTO。未存在または別ユーザーのリソースの場合は null</returns>
    public async Task<CategoryDto?> ExecuteAsync(Guid userId, Guid categoryId, CancellationToken ct = default)
    {
        var category = await categoryRepository.FindByIdAsync(categoryId, ct);
        if (category is null || category.UserId != userId)
            return null;

        return new CategoryDto(category.Id, category.Name, category.Type, category.CreatedAt);
    }
}
