using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Transactions;
using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Transactions;

public class DeleteTransactionUseCaseTests
{
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();

    private DeleteTransactionUseCase CreateSut() => new(_transactionRepository);

    [Fact]
    public async Task ExecuteAsync_WithExistingTransaction_DeletesAndReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var transaction = Transaction.Create(userId, Guid.NewGuid(), 1000, TransactionType.Expense, new DateOnly(2026, 7, 1));

        _transactionRepository.FindByIdAsync(transaction.Id).Returns(transaction);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, transaction.Id);

        result.Should().BeTrue();
        await _transactionRepository.Received(1).DeleteAsync(transaction);
        await _transactionRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentId_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        _transactionRepository.FindByIdAsync(transactionId).Returns((Transaction?)null);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, transactionId);

        result.Should().BeFalse();
        await _transactionRepository.DidNotReceive().DeleteAsync(Arg.Any<Transaction>());
    }

    [Fact]
    public async Task ExecuteAsync_WithOtherUsersTransaction_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var transaction = Transaction.Create(otherUserId, Guid.NewGuid(), 1000, TransactionType.Expense, new DateOnly(2026, 7, 1));

        _transactionRepository.FindByIdAsync(transaction.Id).Returns(transaction);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, transaction.Id);

        result.Should().BeFalse();
        await _transactionRepository.DidNotReceive().DeleteAsync(Arg.Any<Transaction>());
    }
}
