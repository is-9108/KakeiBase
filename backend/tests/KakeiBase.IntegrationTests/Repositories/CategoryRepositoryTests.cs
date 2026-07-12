using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;
using KakeiBase.WebApi.Infrastructure.Persistence;
using KakeiBase.WebApi.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace KakeiBase.IntegrationTests.Repositories;

public class CategoryRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private KakeiBaseDbContext _dbContext = null!;
    private CategoryRepository _sut = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<KakeiBaseDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _dbContext = new KakeiBaseDbContext(options);
        await _dbContext.Database.MigrateAsync();
        _sut = new CategoryRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task<User> CreateUserAsync(string email = "test@example.com")
    {
        var user = User.Create(email, "hashed_password");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task AddAsync_AndFindByIdAsync_ReturnsCreatedCategory()
    {
        var user = await CreateUserAsync();
        var category = Category.Create(user.Id, "食費", TransactionType.Expense);

        await _sut.AddAsync(category);
        await _sut.SaveChangesAsync();

        var found = await _sut.FindByIdAsync(category.Id);

        Assert.NotNull(found);
        Assert.Equal("食費", found.Name);
        Assert.Equal(TransactionType.Expense, found.Type);
        Assert.Equal(user.Id, found.UserId);
    }

    [Fact]
    public async Task FindAllByUserIdAsync_ReturnsOnlyOwnCategories()
    {
        var user = await CreateUserAsync("user1@example.com");
        var otherUser = await CreateUserAsync("user2@example.com");

        var own1 = Category.Create(user.Id, "食費", TransactionType.Expense);
        var own2 = Category.Create(user.Id, "給与", TransactionType.Income);
        var other = Category.Create(otherUser.Id, "交通費", TransactionType.Expense);

        await _sut.AddAsync(own1);
        await _sut.AddAsync(own2);
        await _sut.AddAsync(other);
        await _sut.SaveChangesAsync();

        var results = await _sut.FindAllByUserIdAsync(user.Id);

        Assert.Equal(2, results.Count);
        Assert.All(results, c => Assert.Equal(user.Id, c.UserId));
    }

    [Fact]
    public async Task ExistsByUserIdAndNameAndTypeAsync_WithExistingEntry_ReturnsTrue()
    {
        var user = await CreateUserAsync();
        var category = Category.Create(user.Id, "食費", TransactionType.Expense);

        await _sut.AddAsync(category);
        await _sut.SaveChangesAsync();

        var exists = await _sut.ExistsByUserIdAndNameAndTypeAsync(user.Id, "食費", TransactionType.Expense);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByUserIdAndNameAndTypeAsync_WithExcludeId_ExcludesSelf()
    {
        var user = await CreateUserAsync();
        var category = Category.Create(user.Id, "食費", TransactionType.Expense);

        await _sut.AddAsync(category);
        await _sut.SaveChangesAsync();

        var exists = await _sut.ExistsByUserIdAndNameAndTypeAsync(user.Id, "食費", TransactionType.Expense, excludeId: category.Id);

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsByUserIdAndNameAndTypeAsync_WithOtherUsersSameEntry_ReturnsFalse()
    {
        var user = await CreateUserAsync("user1@example.com");
        var otherUser = await CreateUserAsync("user2@example.com");
        var other = Category.Create(otherUser.Id, "食費", TransactionType.Expense);

        await _sut.AddAsync(other);
        await _sut.SaveChangesAsync();

        var exists = await _sut.ExistsByUserIdAndNameAndTypeAsync(user.Id, "食費", TransactionType.Expense);

        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteAsync_RemovesCategory()
    {
        var user = await CreateUserAsync();
        var category = Category.Create(user.Id, "食費", TransactionType.Expense);

        await _sut.AddAsync(category);
        await _sut.SaveChangesAsync();

        await _sut.DeleteAsync(category);
        await _sut.SaveChangesAsync();

        var found = await _sut.FindByIdAsync(category.Id);
        Assert.Null(found);
    }
}
