using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Categories;

public class DeleteCategoryUseCase(ICategoryRepository categoryRepository)
{
    public async Task<bool> ExecuteAsync(Guid userId, Guid categoryId, CancellationToken ct = default)
    {
        var category = await categoryRepository.FindByIdAsync(categoryId, ct);
        if (category is null || category.UserId != userId)
            return false;

        await categoryRepository.DeleteAsync(category, ct);
        await categoryRepository.SaveChangesAsync(ct);

        return true;
    }
}
