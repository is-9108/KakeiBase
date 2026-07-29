using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Transactions;
using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Transactions;

public class GetTransactionsUseCaseTests
{
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();

    private GetTransactionsUseCase CreateSut() => new(_transactionRepository);

    /// <summary>
    /// Category ナビゲーションプロパティを設定したテスト用 Transaction を生成する。
    /// 本番では EF Core の Include がリフレクション経由でセットするため、ここでも同様に再現する。
    /// </summary>
    private static Transaction CreateTransactionWithCategory(Guid userId, string categoryName = "食費")
    {
        var category = Category.Create(userId, categoryName, TransactionType.Expense);
        var tx = Transaction.Create(userId, category.Id, 1000, new DateOnly(2026, 7, 1));
        typeof(Transaction).GetProperty(nameof(Transaction.Category))!.SetValue(tx, category);
        return tx;
    }

    [Fact]
    public async Task ExecuteAsync_WithNoFilter_ReturnsAllTransactions()
    {
        var userId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            CreateTransactionWithCategory(userId, "食費"),
            CreateTransactionWithCategory(userId, "交通費"),
        };

        _transactionRepository
            .FindAllByUserIdAsync(userId, null, null, null)
            .Returns(transactions);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, null, null, null);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithYearMonthFilter_ReturnsFilteredTransactions()
    {
        var userId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            CreateTransactionWithCategory(userId),
        };

        _transactionRepository
            .FindAllByUserIdAsync(userId, 2026, 7, null)
            .Returns(transactions);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, 2026, 7, null);

        result.Should().HaveCount(1);
        result[0].Date.Year.Should().Be(2026);
        result[0].Date.Month.Should().Be(7);
    }
}
