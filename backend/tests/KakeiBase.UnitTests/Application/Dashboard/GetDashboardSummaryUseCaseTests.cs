using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Dashboard;
using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Dashboard;

public class GetDashboardSummaryUseCaseTests
{
    private readonly ITransactionRepository _transactionRepository =
        Substitute.For<ITransactionRepository>();
    private readonly ICategoryRepository _categoryRepository =
        Substitute.For<ICategoryRepository>();

    private GetDashboardSummaryUseCase CreateSut() =>
        new(_transactionRepository, _categoryRepository);

    private static readonly Guid UserId = Guid.NewGuid();
    private const int Year = 2026;
    private const int Month = 7;

    [Fact]
    public async Task ExecuteAsync_WithNoTransactions_ReturnsZeroSummary()
    {
        _transactionRepository
            .FindAllByUserIdAsync(UserId, Year, Month, null)
            .Returns([]);
        _categoryRepository
            .FindAllByUserIdAsync(UserId)
            .Returns([]);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(UserId, Year, Month);

        result.TotalIncome.Should().Be(0);
        result.TotalExpense.Should().Be(0);
        result.Balance.Should().Be(0);
        result.CategoryBreakdown.Should().BeEmpty();
        result.RecentTransactions.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithMixedTransactions_ReturnsTotalsCorrectly()
    {
        var categoryId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            Transaction.Create(UserId, categoryId, 200_000, TransactionType.Income,  new DateOnly(2026, 7, 25)),
            Transaction.Create(UserId, categoryId,  40_000, TransactionType.Expense, new DateOnly(2026, 7, 20)),
            Transaction.Create(UserId, categoryId, 110_000, TransactionType.Expense, new DateOnly(2026, 7, 10)),
        };

        _transactionRepository
            .FindAllByUserIdAsync(UserId, Year, Month, null)
            .Returns(transactions);
        _categoryRepository
            .FindAllByUserIdAsync(UserId)
            .Returns([Category.Create(UserId, "食費", TransactionType.Expense)]);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(UserId, Year, Month);

        result.TotalIncome.Should().Be(200_000);
        result.TotalExpense.Should().Be(150_000);
    }

    [Fact]
    public async Task ExecuteAsync_CalculatesBalance_IncomeMinusExpense()
    {
        var categoryId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            Transaction.Create(UserId, categoryId, 200_000, TransactionType.Income,  new DateOnly(2026, 7, 1)),
            Transaction.Create(UserId, categoryId, 150_000, TransactionType.Expense, new DateOnly(2026, 7, 2)),
        };

        _transactionRepository
            .FindAllByUserIdAsync(UserId, Year, Month, null)
            .Returns(transactions);
        _categoryRepository
            .FindAllByUserIdAsync(UserId)
            .Returns([]);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(UserId, Year, Month);

        result.Balance.Should().Be(50_000);
    }

    [Fact]
    public async Task ExecuteAsync_CategoryBreakdown_ContainsOnlyExpenses()
    {
        var expenseCategoryId = Guid.NewGuid();
        var incomeCategoryId  = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            Transaction.Create(UserId, incomeCategoryId,  200_000, TransactionType.Income,  new DateOnly(2026, 7, 1)),
            Transaction.Create(UserId, expenseCategoryId,  40_000, TransactionType.Expense, new DateOnly(2026, 7, 2)),
        };

        _transactionRepository
            .FindAllByUserIdAsync(UserId, Year, Month, null)
            .Returns(transactions);
        _categoryRepository
            .FindAllByUserIdAsync(UserId)
            .Returns(
            [
                Category.Create(UserId, "給与", TransactionType.Income),
                Category.Create(UserId, "食費", TransactionType.Expense),
            ]);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(UserId, Year, Month);

        result.CategoryBreakdown.Should().HaveCount(1);
        result.CategoryBreakdown.All(b => b.Amount > 0).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_CategoryBreakdown_PercentageSumsTo100()
    {
        var catA = Guid.NewGuid();
        var catB = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            Transaction.Create(UserId, catA, 30_000, TransactionType.Expense, new DateOnly(2026, 7, 1)),
            Transaction.Create(UserId, catB, 70_000, TransactionType.Expense, new DateOnly(2026, 7, 2)),
        };

        _transactionRepository
            .FindAllByUserIdAsync(UserId, Year, Month, null)
            .Returns(transactions);
        _categoryRepository
            .FindAllByUserIdAsync(UserId)
            .Returns([]);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(UserId, Year, Month);

        var total = result.CategoryBreakdown.Sum(b => b.Percentage);
        // 小数点丸め誤差を考慮して ±0.2 以内に収まることを検証
        total.Should().BeApproximately(100m, 0.2m);
    }

    [Fact]
    public async Task ExecuteAsync_RecentTransactions_LimitedToFive()
    {
        var categoryId = Guid.NewGuid();
        // 6 件（リポジトリは date 降順で返すことを想定）
        var transactions = Enumerable.Range(1, 6)
            .Select(i => Transaction.Create(
                UserId, categoryId, i * 1000, TransactionType.Expense,
                new DateOnly(2026, 7, i)))
            .Reverse() // 降順
            .ToList();

        _transactionRepository
            .FindAllByUserIdAsync(UserId, Year, Month, null)
            .Returns(transactions);
        _categoryRepository
            .FindAllByUserIdAsync(UserId)
            .Returns([]);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(UserId, Year, Month);

        result.RecentTransactions.Should().HaveCount(5);
    }

    [Fact]
    public async Task ExecuteAsync_CategoryBreakdown_WhenNoExpense_PercentageIsZero()
    {
        var categoryId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            Transaction.Create(UserId, categoryId, 200_000, TransactionType.Income, new DateOnly(2026, 7, 1)),
        };

        _transactionRepository
            .FindAllByUserIdAsync(UserId, Year, Month, null)
            .Returns(transactions);
        _categoryRepository
            .FindAllByUserIdAsync(UserId)
            .Returns([]);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(UserId, Year, Month);

        result.CategoryBreakdown.Should().BeEmpty();
        result.TotalExpense.Should().Be(0);
    }
}
