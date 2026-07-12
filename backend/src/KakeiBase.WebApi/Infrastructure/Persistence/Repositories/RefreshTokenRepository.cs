using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KakeiBase.WebApi.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(KakeiBaseDbContext dbContext) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
        => await dbContext.RefreshTokens.AddAsync(token, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => dbContext.SaveChangesAsync(ct);
}
