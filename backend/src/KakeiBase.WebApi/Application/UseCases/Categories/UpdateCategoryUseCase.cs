using KakeiBase.WebApi.Application.DTOs.Categories;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Application.UseCases.Categories;

public record UpdateCategoryResult(CategoryDto? Category, bool IsConflict)
{
    public static UpdateCategoryResult NotFound() => new(null, false);
    public static UpdateCategoryResult Conflict() => new(null, true);
    public static UpdateCategoryResult Success(CategoryDto dto) => new(dto, false);
}

public class UpdateCategoryUseCase(ICategoryRepository categoryRepository)
{
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
