using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Categories;
using KakeiBase.WebApi.Domain.Enums;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Categories;

public class CreateCategoryUseCaseTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();

    private CreateCategoryUseCase CreateSut() => new(_categoryRepository);

    [Fact]
    public async Task ExecuteAsync_WithUniqueNameAndType_ReturnsCategoryDto()
    {
        var userId = Guid.NewGuid();
        _categoryRepository
            .ExistsByUserIdAndNameAndTypeAsync(userId, "食費", TransactionType.Expense, ct: default)
            .Returns(false);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, "食費", TransactionType.Expense);

        result.Should().NotBeNull();
        result!.Name.Should().Be("食費");
        result.Type.Should().Be(TransactionType.Expense);
        await _categoryRepository.Received(1).AddAsync(Arg.Any<KakeiBase.WebApi.Domain.Entities.Category>());
        await _categoryRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicateNameAndType_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        _categoryRepository
            .ExistsByUserIdAndNameAndTypeAsync(userId, "食費", TransactionType.Expense, ct: default)
            .Returns(true);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId, "食費", TransactionType.Expense);

        result.Should().BeNull();
        await _categoryRepository.DidNotReceive().AddAsync(Arg.Any<KakeiBase.WebApi.Domain.Entities.Category>());
    }
}
