using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Categories;
using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Categories;

public class UpdateCategoryUseCaseTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();

    private UpdateCategoryUseCase CreateSut() => new(_categoryRepository);

    private static Category CreateCategory(Guid userId)
        => Category.Create(userId, "食費", TransactionType.Expense);

    [Fact]
    public async Task ExecuteAsync_WithValidUpdate_ReturnsUpdatedDto()
    {
        var userId = Guid.NewGuid();
        var category = CreateCategory(userId);

        _categoryRepository.FindByIdAsync(category.Id).Returns(category);
        _categoryRepository
            .ExistsByUserIdAndNameAndTypeAsync(userId, "交通費", TransactionType.Expense, excludeId: category.Id)
            .Returns(false);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, category.Id, "交通費", TransactionType.Expense);

        result.Category.Should().NotBeNull();
        result.IsConflict.Should().BeFalse();
        result.Category!.Name.Should().Be("交通費");
        await _categoryRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentId_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        _categoryRepository.FindByIdAsync(categoryId).Returns((Category?)null);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, categoryId, "食費", TransactionType.Expense);

        result.Category.Should().BeNull();
        result.IsConflict.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicateNameAndType_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var category = CreateCategory(userId);

        _categoryRepository.FindByIdAsync(category.Id).Returns(category);
        _categoryRepository
            .ExistsByUserIdAndNameAndTypeAsync(userId, "交通費", TransactionType.Expense, excludeId: category.Id)
            .Returns(true);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, category.Id, "交通費", TransactionType.Expense);

        result.Category.Should().BeNull();
        result.IsConflict.Should().BeTrue();
        await _categoryRepository.DidNotReceive().SaveChangesAsync();
    }
}
