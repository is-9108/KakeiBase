using Amazon.S3;
using FluentAssertions;
using KakeiBase.WebApi.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace KakeiBase.UnitTests.Infrastructure.Storage;

public class S3ReceiptStorageServiceTests
{
    private static S3ReceiptStorageService CreateSut(string bucketName)
    {
        var s3Client = Substitute.For<IAmazonS3>();
        var config = Substitute.For<IConfiguration>();
        config["Aws:S3:ReceiptBucket"].Returns(bucketName);
        return new S3ReceiptStorageService(s3Client, config);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GeneratePresignedPutUrlAsync_WhenBucketNameNotConfigured_ThrowsInvalidOperationException(string? bucketName)
    {
        var sut = CreateSut(bucketName!);

        var act = () => sut.GeneratePresignedPutUrlAsync("receipts/uid/uuid.jpg", DateTime.UtcNow.AddMinutes(5));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Aws:S3:ReceiptBucket*");
    }
}
