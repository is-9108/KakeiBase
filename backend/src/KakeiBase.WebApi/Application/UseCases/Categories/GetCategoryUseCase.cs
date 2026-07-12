using KakeiBase.WebApi.Application.DTOs.Categories;
using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Categories;

public class GetCategoryUseCase(ICategoryRepository categoryRepository)
{
    public async Task<CategoryDto?> ExecuteAsync(Guid userId, Guid categoryId, CancellationToken ct = default)
    {
        var category = await categoryRepository.FindByIdAsync(categoryId, ct);
        if (category is null || category.UserId != userId)
            return null;

        return new CategoryDto(category.Id, category.Name, category.Type, category.CreatedAt);
    }
}
