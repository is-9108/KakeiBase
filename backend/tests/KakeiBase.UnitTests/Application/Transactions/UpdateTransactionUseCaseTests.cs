using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Transactions;
using KakeiBase.WebApi.Domain.Entities;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Transactions;

public class UpdateTransactionUseCaseTests
{
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();

    private UpdateTransactionUseCase CreateSut() => new(_transactionRepository);

    private static Transaction CreateTransaction(Guid userId)
        => Transaction.Create(userId, Guid.NewGuid(), 1000, new DateOnly(2026, 7, 1));

    [Fact]
    public async Task ExecuteAsync_WithValidUpdate_ReturnsUpdatedDto()
    {
        var userId = Guid.NewGuid();
        var transaction = CreateTransaction(userId);
        var newCategoryId = Guid.NewGuid();
        var newDate = new DateOnly(2026, 7, 15);

        _transactionRepository.FindByIdAsync(transaction.Id).Returns(transaction);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, transaction.Id, newCategoryId, 2000, newDate, "更新", null);

        result.IsNotFound.Should().BeFalse();
        result.Transaction.Should().NotBeNull();
        result.Transaction!.Amount.Should().Be(2000);
        result.Transaction.CategoryId.Should().Be(newCategoryId);
        await _transactionRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentId_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        _transactionRepository.FindByIdAsync(transactionId).Returns((Transaction?)null);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, transactionId, Guid.NewGuid(), 1000, new DateOnly(2026, 7, 1), null, null);

        result.IsNotFound.Should().BeTrue();
        result.Transaction.Should().BeNull();
        await _transactionRepository.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WithOtherUsersTransaction_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var transaction = CreateTransaction(otherUserId);

        _transactionRepository.FindByIdAsync(transaction.Id).Returns(transaction);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, transaction.Id, Guid.NewGuid(), 1000, new DateOnly(2026, 7, 1), null, null);

        result.IsNotFound.Should().BeTrue();
        result.Transaction.Should().BeNull();
        await _transactionRepository.DidNotReceive().SaveChangesAsync();
    }
}
