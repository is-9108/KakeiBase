using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Categories;
using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Categories;

public class DeleteCategoryUseCaseTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();

    private DeleteCategoryUseCase CreateSut() => new(_categoryRepository);

    [Fact]
    public async Task ExecuteAsync_WithExistingCategory_DeletesAndReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var category = Category.Create(userId, "食費", TransactionType.Expense);

        _categoryRepository.FindByIdAsync(category.Id).Returns(category);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, category.Id);

        result.Should().BeTrue();
        await _categoryRepository.Received(1).DeleteAsync(category);
        await _categoryRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentId_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        _categoryRepository.FindByIdAsync(categoryId).Returns((Category?)null);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, categoryId);

        result.Should().BeFalse();
        await _categoryRepository.DidNotReceive().DeleteAsync(Arg.Any<Category>());
    }

    [Fact]
    public async Task ExecuteAsync_WithOtherUsersCategory_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var category = Category.Create(otherUserId, "食費", TransactionType.Expense);

        _categoryRepository.FindByIdAsync(category.Id).Returns(category);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, category.Id);

        result.Should().BeFalse();
        await _categoryRepository.DidNotReceive().DeleteAsync(Arg.Any<Category>());
    }
}
