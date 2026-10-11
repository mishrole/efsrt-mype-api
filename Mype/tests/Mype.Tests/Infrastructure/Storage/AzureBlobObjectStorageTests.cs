using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Moq;
using Mype.Application.Common.Models;
using Mype.Infrastructure.Storage;

namespace Mype.Tests.Infrastructure.Storage
{
    public sealed class AzureBlobObjectStorageTests
    {
        private const string AccountName = "mypeaccount";
        private const string ContainerName = "evidences";
        private const string ObjectKey =
            "businesses/business/movements/movement/evidences/evidence/token.pdf";

        [Fact]
        public async Task UploadAsync_Should_Upload_With_ContentType_And_IfNoneMatch()
        {
            var fixture = new Fixture();
            BlobUploadOptions capturedOptions = null;
            var contentInfo = BlobsModelFactory.BlobContentInfo(eTag: new ETag("\"etag-value\""));
            fixture
                .Blob.Setup(client =>
                    client.UploadAsync(
                        It.IsAny<Stream>(),
                        It.IsAny<BlobUploadOptions>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Callback(
                    (Stream _, BlobUploadOptions options, CancellationToken _) =>
                        capturedOptions = options
                )
                .ReturnsAsync(Response.FromValue(contentInfo, Mock.Of<Response>()));
            using var content = new MemoryStream([0x25, 0x50, 0x44, 0x46, 0x2D]);
            var upload = new StorageUpload(ObjectKey, "application/pdf", content.Length, content);

            var result = await fixture.Storage.UploadAsync(upload, CancellationToken.None);

            fixture.Container.Verify(client => client.GetBlobClient(ObjectKey), Times.Once);
            capturedOptions.Should().NotBeNull();
            capturedOptions.HttpHeaders.ContentType.Should().Be("application/pdf");
            capturedOptions.Conditions.IfNoneMatch.Should().Be(ETag.All);
            result.ObjectKey.Should().Be(ObjectKey);
            result.ContentType.Should().Be("application/pdf");
            result.SizeBytes.Should().Be(content.Length);
            result.ETag.Should().Be("\"etag-value\"");
        }

        [Fact]
        public async Task CreateTemporaryReadAccessAsync_Should_Create_Https_ReadOnly_UserDelegationSas()
        {
            var fixture = new Fixture();
            DateTimeOffset capturedStartsOn = default;
            DateTimeOffset capturedExpiresOn = default;
            var before = DateTimeOffset.UtcNow;
            var delegationKey = BlobsModelFactory.UserDelegationKey(
                "11111111-1111-1111-1111-111111111111",
                "22222222-2222-2222-2222-222222222222",
                before.AddMinutes(-2),
                before.AddMinutes(10),
                "b",
                "2023-11-03",
                null,
                Convert.ToBase64String(new byte[32])
            );
            fixture
                .Service.Setup(client =>
                    client.GetUserDelegationKeyAsync(
                        It.IsAny<DateTimeOffset>(),
                        It.IsAny<DateTimeOffset>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Callback(
                    (DateTimeOffset? startsOn, DateTimeOffset expiresOn, CancellationToken _) =>
                    {
                        capturedStartsOn = startsOn.GetValueOrDefault();
                        capturedExpiresOn = expiresOn;
                    }
                )
                .ReturnsAsync(Response.FromValue(delegationKey, Mock.Of<Response>()));

            var result = await fixture.Storage.CreateTemporaryReadAccessAsync(
                ObjectKey,
                TimeSpan.FromMinutes(5),
                CancellationToken.None
            );
            var after = DateTimeOffset.UtcNow;
            var query = result.Url.Query;

            capturedStartsOn.Should().BeOnOrAfter(before.AddMinutes(-1).AddSeconds(-1));
            capturedStartsOn.Should().BeOnOrBefore(after.AddMinutes(-1).AddSeconds(1));
            capturedExpiresOn.Should().BeOnOrAfter(before.AddMinutes(5));
            capturedExpiresOn.Should().BeOnOrBefore(after.AddMinutes(5).AddSeconds(1));
            result.ExpiresAt.Should().Be(capturedExpiresOn);
            result.Url.Scheme.Should().Be(Uri.UriSchemeHttps);
            result.Url.AbsolutePath.Should().EndWith("/" + ObjectKey);
            query.Should().Contain("sp=r");
            query.Should().Contain("sr=b");
            query.Should().Contain("spr=https");
            query.Should().NotContain("sig=&");
            query.Should().NotContain("sp=rw");
        }

        [Fact]
        public async Task DeleteIfExistsAsync_Should_Delete_Blob_And_Include_Snapshots()
        {
            var fixture = new Fixture();
            fixture
                .Blob.Setup(client =>
                    client.DeleteIfExistsAsync(
                        DeleteSnapshotsOption.IncludeSnapshots,
                        null,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            await fixture.Storage.DeleteIfExistsAsync(ObjectKey, CancellationToken.None);

            fixture.Container.Verify(client => client.GetBlobClient(ObjectKey), Times.Once);
            fixture.Blob.Verify(
                client =>
                    client.DeleteIfExistsAsync(
                        DeleteSnapshotsOption.IncludeSnapshots,
                        null,
                        CancellationToken.None
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task UploadAsync_Should_Reject_Invalid_Arguments_Before_Calling_Azure()
        {
            var fixture = new Fixture();
            using var content = new MemoryStream([1]);

            await FluentActions
                .Invoking(() => fixture.Storage.UploadAsync(null, CancellationToken.None))
                .Should()
                .ThrowAsync<ArgumentNullException>();
            await FluentActions
                .Invoking(() =>
                    fixture.Storage.UploadAsync(
                        new StorageUpload("", "application/pdf", 1, content),
                        CancellationToken.None
                    )
                )
                .Should()
                .ThrowAsync<ArgumentException>();
            await FluentActions
                .Invoking(() =>
                    fixture.Storage.UploadAsync(
                        new StorageUpload(ObjectKey, "", 1, content),
                        CancellationToken.None
                    )
                )
                .Should()
                .ThrowAsync<ArgumentException>();
            await FluentActions
                .Invoking(() =>
                    fixture.Storage.UploadAsync(
                        new StorageUpload(ObjectKey, "application/pdf", 0, content),
                        CancellationToken.None
                    )
                )
                .Should()
                .ThrowAsync<ArgumentOutOfRangeException>();
            fixture.Container.Verify(
                client => client.GetBlobClient(It.IsAny<string>()),
                Times.Never
            );
        }

        [Fact]
        public async Task TemporaryAccess_And_Delete_Should_Reject_Invalid_Arguments()
        {
            var fixture = new Fixture();

            await FluentActions
                .Invoking(() =>
                    fixture.Storage.CreateTemporaryReadAccessAsync(
                        "",
                        TimeSpan.FromMinutes(5),
                        CancellationToken.None
                    )
                )
                .Should()
                .ThrowAsync<ArgumentException>();
            await FluentActions
                .Invoking(() =>
                    fixture.Storage.CreateTemporaryReadAccessAsync(
                        ObjectKey,
                        TimeSpan.Zero,
                        CancellationToken.None
                    )
                )
                .Should()
                .ThrowAsync<ArgumentOutOfRangeException>();
            await FluentActions
                .Invoking(() => fixture.Storage.DeleteIfExistsAsync("", CancellationToken.None))
                .Should()
                .ThrowAsync<ArgumentException>();
            fixture.Service.Verify(
                client =>
                    client.GetUserDelegationKeyAsync(
                        It.IsAny<DateTimeOffset>(),
                        It.IsAny<DateTimeOffset>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        private sealed class Fixture
        {
            public Mock<BlobServiceClient> Service { get; } = new();
            public Mock<BlobContainerClient> Container { get; } = new();
            public Mock<BlobClient> Blob { get; } = new();
            public AzureBlobObjectStorage Storage { get; }

            public Fixture()
            {
                Service.SetupGet(client => client.AccountName).Returns(AccountName);
                Container.SetupGet(client => client.Name).Returns(ContainerName);
                Container
                    .Setup(client => client.GetBlobClient(It.IsAny<string>()))
                    .Returns(Blob.Object);
                Blob.SetupGet(client => client.Uri)
                    .Returns(
                        new Uri(
                            $"https://{AccountName}.blob.core.windows.net/{ContainerName}/{ObjectKey}"
                        )
                    );
                Storage = new AzureBlobObjectStorage(Service.Object, Container.Object);
            }
        }
    }
}
