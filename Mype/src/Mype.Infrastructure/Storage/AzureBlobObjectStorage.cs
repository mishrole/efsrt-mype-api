using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Mype.Application.Common.Interfaces;
using Mype.Application.Common.Models;

namespace Mype.Infrastructure.Storage
{
    public sealed class AzureBlobObjectStorage : IObjectStorage
    {
        private readonly BlobServiceClient _serviceClient;
        private readonly BlobContainerClient _containerClient;

        public AzureBlobObjectStorage(
            BlobServiceClient serviceClient,
            BlobContainerClient containerClient
        )
        {
            _serviceClient = serviceClient;
            _containerClient = containerClient;
        }

        public async Task<StoredObjectMetadata> UploadAsync(
            StorageUpload upload,
            CancellationToken cancellationToken
        )
        {
            ArgumentNullException.ThrowIfNull(upload);
            ArgumentException.ThrowIfNullOrWhiteSpace(upload.ObjectKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(upload.ContentType);
            ArgumentNullException.ThrowIfNull(upload.Content);

            if (upload.Length <= 0)
                throw new ArgumentOutOfRangeException(nameof(upload.Length));

            var blobClient = _containerClient.GetBlobClient(upload.ObjectKey);
            var response = await blobClient.UploadAsync(
                upload.Content,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = upload.ContentType },
                    Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
                },
                cancellationToken
            );

            return new StoredObjectMetadata(
                upload.ObjectKey,
                upload.ContentType,
                upload.Length,
                response.Value.ETag.ToString()
            );
        }

        public async Task<TemporaryObjectAccessResult> CreateTemporaryReadAccessAsync(
            string objectKey,
            TimeSpan lifetime,
            CancellationToken cancellationToken
        )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
            if (lifetime <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(lifetime));

            var startsOn = DateTimeOffset.UtcNow.AddMinutes(-1);
            var expiresAt = DateTimeOffset.UtcNow.Add(lifetime);
            var delegationKeyResponse = await _serviceClient.GetUserDelegationKeyAsync(
                startsOn,
                expiresAt,
                cancellationToken
            );
            var blobClient = _containerClient.GetBlobClient(objectKey);
            var sasBuilder = new BlobSasBuilder(BlobSasPermissions.Read, expiresAt)
            {
                BlobContainerName = _containerClient.Name,
                BlobName = objectKey,
                Resource = "b",
                StartsOn = startsOn,
                Protocol = SasProtocol.Https,
            };
            var sas = sasBuilder.ToSasQueryParameters(
                delegationKeyResponse.Value,
                _serviceClient.AccountName
            );
            var uriBuilder = new BlobUriBuilder(blobClient.Uri) { Sas = sas };

            return new TemporaryObjectAccessResult(uriBuilder.ToUri(), expiresAt);
        }

        public async Task DeleteIfExistsAsync(
            string objectKey,
            CancellationToken cancellationToken
        )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);

            await _containerClient
                .GetBlobClient(objectKey)
                .DeleteIfExistsAsync(
                    DeleteSnapshotsOption.IncludeSnapshots,
                    cancellationToken: cancellationToken
                );
        }
    }
}
