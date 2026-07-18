using KakeiBase.WebApi.Application.DTOs.Categories;
using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Categories;

/// <summary>ユーザーのカテゴリ一覧を取得するユースケース</summary>
public class GetCategoriesUseCase(ICategoryRepository categoryRepository)
{
    /// <param name="userId">取得対象ユーザーのID</param>
    /// <param name="ct">キャンセルトークン</param>
    public async Task<List<CategoryDto>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var categories = await categoryRepository.FindAllByUserIdAsync(userId, ct);
        return categories
            .Select(c => new CategoryDto(c.Id, c.Name, c.Type, c.CreatedAt))
            .ToList();
    }
}
