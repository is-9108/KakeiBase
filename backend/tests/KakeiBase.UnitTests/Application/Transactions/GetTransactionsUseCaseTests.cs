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

    [Fact]
    public async Task ExecuteAsync_WithNoFilter_ReturnsAllTransactions()
    {
        var userId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            Transaction.Create(userId, Guid.NewGuid(), 1000m, TransactionType.Expense, new DateOnly(2026, 7, 1)),
            Transaction.Create(userId, Guid.NewGuid(), 2000m, TransactionType.Income, new DateOnly(2026, 6, 15)),
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
            Transaction.Create(userId, Guid.NewGuid(), 1000m, TransactionType.Expense, new DateOnly(2026, 7, 1)),
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
