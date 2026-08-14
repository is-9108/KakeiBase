using FluentAssertions;
using KakeiBase.WebApi.Application.Interfaces;
using KakeiBase.WebApi.Application.UseCases.Receipts;
using NSubstitute;

namespace KakeiBase.UnitTests.Application.Receipts;

public class GeneratePresignedUrlUseCaseTests
{
    private readonly IReceiptStorageService _receiptStorageService = Substitute.For<IReceiptStorageService>();

    private GeneratePresignedUrlUseCase CreateSut() => new(_receiptStorageService);

    [Fact]
    public async Task ExecuteAsync_ReturnsPresignedUrlDto()
    {
        var userId = Guid.NewGuid();
        var expectedUrl = "https://example.s3.amazonaws.com/receipts/presigned";
        _receiptStorageService
            .GeneratePresignedPutUrlAsync(Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(expectedUrl);

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId);

        result.UploadUrl.Should().Be(expectedUrl);
    }

    [Fact]
    public async Task ExecuteAsync_S3KeyHasCorrectFormat()
    {
        var userId = Guid.NewGuid();
        _receiptStorageService
            .GeneratePresignedPutUrlAsync(Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns("https://example.com/url");

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId);

        // ADR-0010: receipts/{userId}/{yyyy}/{MM}/{yyyyMMdd}_{HHmmss}.{fff}.jpg
        result.S3Key.Should().StartWith($"receipts/{userId}/");
        result.S3Key.Should().EndWith(".jpg");
        var parts = result.S3Key.Split('/');
        parts.Should().HaveCount(5, "キーは receipts/{userId}/{yyyy}/{MM}/{filename}.jpg の5階層");
        parts[2].Should().HaveLength(4, "年は4桁");
        parts[3].Should().HaveLength(2, "月はゼロ埋め2桁");
        parts[4].Should().MatchRegex(@"^\d{8}_\d{6}\.\d{3}\.jpg$", "ファイル名は yyyyMMdd_HHmmss.fff.jpg 形式");
    }

    [Fact]
    public async Task ExecuteAsync_ExpiresAtIsApproximatelyFiveMinutesFromNow()
    {
        var userId = Guid.NewGuid();
        _receiptStorageService
            .GeneratePresignedPutUrlAsync(Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns("https://example.com/url");

        var before = DateTime.UtcNow;
        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId);
        var after = DateTime.UtcNow;

        var expiresAt = DateTimeOffset.Parse(result.ExpiresAt).UtcDateTime;
        expiresAt.Should().BeOnOrAfter(before.AddMinutes(5));
        expiresAt.Should().BeOnOrBefore(after.AddMinutes(5).AddSeconds(1));
    }

    [Fact]
    public async Task ExecuteAsync_DifferentCallsGenerateDifferentS3Keys()
    {
        var userId = Guid.NewGuid();
        _receiptStorageService
            .GeneratePresignedPutUrlAsync(Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns("https://example.com/url");

        var sut = CreateSut();
        var result1 = await sut.ExecuteAsync(userId);
        var result2 = await sut.ExecuteAsync(userId);

        result1.S3Key.Should().NotBe(result2.S3Key);
    }

    [Fact]
    public async Task ExecuteAsync_PassesSameExpiresAtToStorageServiceAndResponse()
    {
        var userId = Guid.NewGuid();
        DateTime? capturedExpiresAt = null;
        _receiptStorageService
            .GeneratePresignedPutUrlAsync(Arg.Any<string>(), Arg.Do<DateTime>(d => capturedExpiresAt = d))
            .Returns("https://example.com/url");

        var sut = CreateSut();
        var result = await sut.ExecuteAsync(userId);

        var responseExpiresAt = DateTimeOffset.Parse(result.ExpiresAt).UtcDateTime;
        capturedExpiresAt.Should().Be(responseExpiresAt, "ストレージサービスに渡した有効期限とレスポンスの ExpiresAt は一致する必要がある");
    }
}
