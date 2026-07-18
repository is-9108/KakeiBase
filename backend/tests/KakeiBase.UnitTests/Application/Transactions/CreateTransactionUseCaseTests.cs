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
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();

    private CreateTransactionUseCase CreateSut() => new(_transactionRepository, _categoryRepository);

    [Fact]
    public async Task ExecuteAsync_WithValidInput_ReturnsTransactionDto()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var date = new DateOnly(2026, 7, 15);
        var category = Category.Create(userId, "食費", TransactionType.Expense);
        _categoryRepository.FindByIdAsync(categoryId).Returns(category);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, categoryId, 1000, date, "テスト", null);

        result.Should().NotBeNull();
        result!.Amount.Should().Be(1000);
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
        var category = Category.Create(userId, "給与", TransactionType.Income);
        _categoryRepository.FindByIdAsync(categoryId).Returns(category);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, categoryId, 500, date, null, null);

        result!.CategoryId.Should().Be(categoryId);
    }

    [Fact]
    public async Task ExecuteAsync_CategoryNotFound_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var date = new DateOnly(2026, 7, 15);
        _categoryRepository.FindByIdAsync(categoryId).Returns((Category?)null);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, categoryId, 1000, date, null, null);

        result.Should().BeNull();
        await _transactionRepository.DidNotReceive().AddAsync(Arg.Any<Transaction>());
    }

    [Fact]
    public async Task ExecuteAsync_CategoryOwnedByOtherUser_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var date = new DateOnly(2026, 7, 15);
        var category = Category.Create(otherUserId, "他ユーザーのカテゴリ", TransactionType.Expense);
        _categoryRepository.FindByIdAsync(categoryId).Returns(category);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, categoryId, 1000, date, null, null);

        result.Should().BeNull();
        await _transactionRepository.DidNotReceive().AddAsync(Arg.Any<Transaction>());
    }
}
