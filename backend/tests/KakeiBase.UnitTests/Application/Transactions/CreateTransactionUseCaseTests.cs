using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Transactions;
using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Transactions;

public class CreateTransactionUseCaseTests
{
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();

    private CreateTransactionUseCase CreateSut() => new(_transactionRepository);

    [Fact]
    public async Task ExecuteAsync_WithValidInput_ReturnsTransactionDto()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var date = new DateOnly(2026, 7, 15);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, categoryId, 1000, TransactionType.Expense, date, "テスト", null);

        result.Should().NotBeNull();
        result.Amount.Should().Be(1000);
        result.Type.Should().Be(TransactionType.Expense);
        result.Date.Should().Be(date);
        result.Memo.Should().Be("テスト");
        await _transactionRepository.Received(1).AddAsync(Arg.Any<Transaction>());
        await _transactionRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_PreservesCategoryId()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var date = new DateOnly(2026, 7, 15);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, categoryId, 500, TransactionType.Income, date, null, null);

        result.CategoryId.Should().Be(categoryId);
    }
}
