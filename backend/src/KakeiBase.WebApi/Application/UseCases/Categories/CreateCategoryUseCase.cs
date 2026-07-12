using KakeiBase.WebApi.Application.DTOs.Categories;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Application.UseCases.Categories;

public class CreateCategoryUseCase(ICategoryRepository categoryRepository)
{
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
