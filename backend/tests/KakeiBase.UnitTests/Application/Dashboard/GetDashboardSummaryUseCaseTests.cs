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
        var incomeCategory = Category.Create(UserId, "給与", TransactionType.Income);
        var expenseCategory = Category.Create(UserId, "食費", TransactionType.Expense);
        var transactions = new List<Transaction>
        {
            Transaction.Create(UserId, incomeCategory.Id,  200_000, new DateOnly(2026, 7, 25)),
            Transaction.Create(UserId, expenseCategory.Id,  40_000, new DateOnly(2026, 7, 20)),
            Transaction.Create(UserId, expenseCategory.Id, 110_000, new DateOnly(2026, 7, 10)),
        };

        _transactionRepository
            .FindAllByUserIdAsync(UserId, Year, Month, null)
            .Returns(transactions);
        _categoryRepository
            .FindAllByUserIdAsync(UserId)
            .Returns([incomeCategory, expenseCategory]);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(UserId, Year, Month);

        result.TotalIncome.Should().Be(200_000);
        result.TotalExpense.Should().Be(150_000);
    }

    [Fact]
    public async Task ExecuteAsync_CalculatesBalance_IncomeMinusExpense()
    {
        var incomeCategory = Category.Create(UserId, "給与", TransactionType.Income);
        var expenseCategory = Category.Create(UserId, "食費", TransactionType.Expense);
        var transactions = new List<Transaction>
        {
            Transaction.Create(UserId, incomeCategory.Id,  200_000, new DateOnly(2026, 7, 1)),
            Transaction.Create(UserId, expenseCategory.Id, 150_000, new DateOnly(2026, 7, 2)),
        };

        _transactionRepository
            .FindAllByUserIdAsync(UserId, Year, Month, null)
            .Returns(transactions);
        _categoryRepository
            .FindAllByUserIdAsync(UserId)
            .Returns([incomeCategory, expenseCategory]);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(UserId, Year, Month);

        result.Balance.Should().Be(50_000);
    }

    [Fact]
    public async Task ExecuteAsync_CategoryBreakdown_ContainsOnlyExpenses()
    {
        var incomeCategory = Category.Create(UserId, "給与", TransactionType.Income);
        var expenseCategory = Category.Create(UserId, "食費", TransactionType.Expense);
        var transactions = new List<Transaction>
        {
            Transaction.Create(UserId, incomeCategory.Id,  200_000, new DateOnly(2026, 7, 1)),
            Transaction.Create(UserId, expenseCategory.Id,  40_000, new DateOnly(2026, 7, 2)),
        };

        _transactionRepository
            .FindAllByUserIdAsync(UserId, Year, Month, null)
            .Returns(transactions);
        _categoryRepository
            .FindAllByUserIdAsync(UserId)
            .Returns([incomeCategory, expenseCategory]);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(UserId, Year, Month);

        result.CategoryBreakdown.Should().HaveCount(1);
        result.CategoryBreakdown.All(b => b.Amount > 0).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_CategoryBreakdown_PercentageSumsTo100()
    {
        var catA = Category.Create(UserId, "食費", TransactionType.Expense);
        var catB = Category.Create(UserId, "交通費", TransactionType.Expense);
        var transactions = new List<Transaction>
        {
            Transaction.Create(UserId, catA.Id, 30_000, new DateOnly(2026, 7, 1)),
            Transaction.Create(UserId, catB.Id, 70_000, new DateOnly(2026, 7, 2)),
        };

        _transactionRepository
            .FindAllByUserIdAsync(UserId, Year, Month, null)
            .Returns(transactions);
        _categoryRepository
            .FindAllByUserIdAsync(UserId)
            .Returns([catA, catB]);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(UserId, Year, Month);

        var total = result.CategoryBreakdown.Sum(b => b.Percentage);
        // 小数点丸め誤差を考慮して ±0.2 以内に収まることを検証
        total.Should().BeApproximately(100m, 0.2m);
    }

    [Fact]
    public async Task ExecuteAsync_RecentTransactions_LimitedToFive()
    {
        var expenseCategory = Category.Create(UserId, "食費", TransactionType.Expense);
        // 6 件（リポジトリは date 降順で返すことを想定）
        var transactions = Enumerable.Range(1, 6)
            .Select(i => Transaction.Create(
                UserId, expenseCategory.Id, i * 1000,
                new DateOnly(2026, 7, i)))
            .Reverse() // 降順
            .ToList();

        _transactionRepository
            .FindAllByUserIdAsync(UserId, Year, Month, null)
            .Returns(transactions);
        _categoryRepository
            .FindAllByUserIdAsync(UserId)
            .Returns([expenseCategory]);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(UserId, Year, Month);

        result.RecentTransactions.Should().HaveCount(5);
    }

    [Fact]
    public async Task ExecuteAsync_CategoryBreakdown_WhenNoExpense_PercentageIsZero()
    {
        var incomeCategory = Category.Create(UserId, "給与", TransactionType.Income);
        var transactions = new List<Transaction>
        {
            Transaction.Create(UserId, incomeCategory.Id, 200_000, new DateOnly(2026, 7, 1)),
        };

        _transactionRepository
            .FindAllByUserIdAsync(UserId, Year, Month, null)
            .Returns(transactions);
        _categoryRepository
            .FindAllByUserIdAsync(UserId)
            .Returns([incomeCategory]);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(UserId, Year, Month);

        result.CategoryBreakdown.Should().BeEmpty();
        result.TotalExpense.Should().Be(0);
    }
}
