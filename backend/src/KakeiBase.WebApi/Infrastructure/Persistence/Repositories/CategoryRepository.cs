using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KakeiBase.WebApi.Infrastructure.Persistence.Repositories;

public class CategoryRepository(KakeiBaseDbContext dbContext) : ICategoryRepository
{
    public Task<Category?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<Category>> FindAllByUserIdAsync(Guid userId, CancellationToken ct = default)
        => dbContext.Categories.Where(c => c.UserId == userId).ToListAsync(ct);

    public Task<bool> ExistsByUserIdAndNameAndTypeAsync(Guid userId, string name, TransactionType type, Guid? excludeId = null, CancellationToken ct = default)
        => dbContext.Categories
            .Where(c => c.UserId == userId && c.Name == name && c.Type == type && (excludeId == null || c.Id != excludeId))
            .AnyAsync(ct);

    public async Task AddAsync(Category category, CancellationToken ct = default)
        => await dbContext.Categories.AddAsync(category, ct);

    public Task DeleteAsync(Category category, CancellationToken ct = default)
    {
        dbContext.Categories.Remove(category);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => dbContext.SaveChangesAsync(ct);
}
