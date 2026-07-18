using KakeiBase.WebApi.Application.DTOs.Categories;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Application.UseCases.Categories;

/// <summary>新しいカテゴリを作成するユースケース</summary>
public class CreateCategoryUseCase(ICategoryRepository categoryRepository)
{
    /// <param name="userId">作成するユーザーのID</param>
    /// <param name="name">カテゴリ名</param>
    /// <param name="type">収支区分</param>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>作成されたカテゴリ。同名・同種別のカテゴリが既に存在する場合は null</returns>
    public async Task<CategoryDto?> ExecuteAsync(Guid userId, string name, TransactionType type, CancellationToken ct = default)
    {
        var exists = await categoryRepository.ExistsByUserIdAndNameAndTypeAsync(userId, name, type, ct: ct);
        if (exists)
            return null;

        var category = Category.Create(userId, name, type);
        await categoryRepository.AddAsync(category, ct);
        await categoryRepository.SaveChangesAsync(ct);

        return new CategoryDto(category.Id, category.Name, category.Type, category.CreatedAt);
    }
}
