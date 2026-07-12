using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;

namespace KakeiBase.WebApi.Application.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Category>> FindAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsByUserIdAndNameAndTypeAsync(Guid userId, string name, TransactionType type, Guid? excludeId = null, CancellationToken ct = default);
    Task AddAsync(Category category, CancellationToken ct = default);
    Task DeleteAsync(Category category, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
