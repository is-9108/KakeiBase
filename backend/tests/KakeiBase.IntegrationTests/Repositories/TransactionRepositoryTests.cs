using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;
using KakeiBase.WebApi.Infrastructure.Persistence;
using KakeiBase.WebApi.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace KakeiBase.IntegrationTests.Repositories;

public class TransactionRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private KakeiBaseDbContext _dbContext = null!;
    private TransactionRepository _sut = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<KakeiBaseDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _dbContext = new KakeiBaseDbContext(options);
        await _dbContext.Database.MigrateAsync();
        _sut = new TransactionRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task<(User user, Category category)> CreateUserAndCategoryAsync(string email = "test@example.com")
    {
        var user = User.Create(email, "hashed_password");
        await _dbContext.Users.AddAsync(user);
        var category = Category.Create(user.Id, "食費", TransactionType.Expense);
        await _dbContext.Categories.AddAsync(category);
        await _dbContext.SaveChangesAsync();
        return (user, category);
    }

    [Fact]
    public async Task AddAsync_AndFindByIdAsync_ReturnsCreatedTransaction()
    {
        var (user, category) = await CreateUserAndCategoryAsync();
        var transaction = Transaction.Create(user.Id, category.Id, 1000, new DateOnly(2026, 7, 1), "ランチ");

        await _sut.AddAsync(transaction);
        await _sut.SaveChangesAsync();

        var found = await _sut.FindByIdAsync(transaction.Id);

        Assert.NotNull(found);
        Assert.Equal(1000, found.Amount);
        Assert.Equal("ランチ", found.Memo);
        Assert.Equal(user.Id, found.UserId);
        Assert.Equal(category.Id, found.CategoryId);
    }

    [Fact]
    public async Task FindAllByUserIdAsync_ReturnsOnlyOwnTransactions()
    {
        var (user, category) = await CreateUserAndCategoryAsync("user1@example.com");
        var (otherUser, otherCategory) = await CreateUserAndCategoryAsync("user2@example.com");

        var t1 = Transaction.Create(user.Id, category.Id, 1000, new DateOnly(2026, 7, 1));
        var t2 = Transaction.Create(user.Id, category.Id, 2000, new DateOnly(2026, 7, 2));
        var t3 = Transaction.Create(otherUser.Id, otherCategory.Id, 3000, new DateOnly(2026, 7, 3));

        await _sut.AddAsync(t1);
        await _sut.AddAsync(t2);
        await _sut.AddAsync(t3);
        await _sut.SaveChangesAsync();

        var results = await _sut.FindAllByUserIdAsync(user.Id, null, null, null);

        Assert.Equal(2, results.Count);
        Assert.All(results, t => Assert.Equal(user.Id, t.UserId));
    }

    [Fact]
    public async Task FindAllByUserIdAsync_WithYearMonthFilter_ReturnsOnlyMatchingTransactions()
    {
        var (user, category) = await CreateUserAndCategoryAsync();

        var julyTx = Transaction.Create(user.Id, category.Id, 1000, new DateOnly(2026, 7, 15));
        var juneTx = Transaction.Create(user.Id, category.Id, 2000, new DateOnly(2026, 6, 10));

        await _sut.AddAsync(julyTx);
        await _sut.AddAsync(juneTx);
        await _sut.SaveChangesAsync();

        var results = await _sut.FindAllByUserIdAsync(user.Id, 2026, 7, null);

        Assert.Single(results);
        Assert.Equal(julyTx.Id, results[0].Id);
    }

    [Fact]
    public async Task FindAllByUserIdAsync_WithCategoryFilter_ReturnsOnlyMatchingTransactions()
    {
        var (user, category) = await CreateUserAndCategoryAsync();
        var otherCategory = Category.Create(user.Id, "交通費", TransactionType.Expense);
        await _dbContext.Categories.AddAsync(otherCategory);
        await _dbContext.SaveChangesAsync();

        var t1 = Transaction.Create(user.Id, category.Id, 1000, new DateOnly(2026, 7, 1));
        var t2 = Transaction.Create(user.Id, otherCategory.Id, 500, new DateOnly(2026, 7, 2));

        await _sut.AddAsync(t1);
        await _sut.AddAsync(t2);
        await _sut.SaveChangesAsync();

        var results = await _sut.FindAllByUserIdAsync(user.Id, null, null, category.Id);

        Assert.Single(results);
        Assert.Equal(category.Id, results[0].CategoryId);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTransaction()
    {
        var (user, category) = await CreateUserAndCategoryAsync();
        var transaction = Transaction.Create(user.Id, category.Id, 1000, new DateOnly(2026, 7, 1));

        await _sut.AddAsync(transaction);
        await _sut.SaveChangesAsync();

        await _sut.DeleteAsync(transaction);
        await _sut.SaveChangesAsync();

        var found = await _sut.FindByIdAsync(transaction.Id);
        Assert.Null(found);
    }
}
