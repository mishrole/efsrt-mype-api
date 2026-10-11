using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mype.Application.Common.Interfaces;
using Mype.Application.Common.Models;

namespace Mype.Tests.Integration
{
    public sealed class FakeObjectStorage : IObjectStorage
    {
        private readonly ConcurrentDictionary<string, byte[]> _objects = new();

        public async Task<StoredObjectMetadata> UploadAsync(
            StorageUpload upload,
            CancellationToken cancellationToken
        )
        {
            using var buffer = new MemoryStream();
            await upload.Content.CopyToAsync(buffer, cancellationToken);
            if (!_objects.TryAdd(upload.ObjectKey, buffer.ToArray()))
                throw new InvalidOperationException("The object already exists.");

            return new StoredObjectMetadata(
                upload.ObjectKey,
                upload.ContentType,
                upload.Length,
                "fake-etag"
            );
        }

        public Task<TemporaryObjectAccessResult> CreateTemporaryReadAccessAsync(
            string objectKey,
            TimeSpan lifetime,
            CancellationToken cancellationToken
        )
        {
            if (!_objects.ContainsKey(objectKey))
                throw new FileNotFoundException("The object does not exist.", objectKey);

            var expiresAt = DateTimeOffset.UtcNow.Add(lifetime);
            return Task.FromResult(
                new TemporaryObjectAccessResult(
                    new Uri($"https://storage.test/{Uri.EscapeDataString(objectKey)}"),
                    expiresAt
                )
            );
        }

        public Task DeleteIfExistsAsync(string objectKey, CancellationToken cancellationToken)
        {
            _objects.TryRemove(objectKey, out _);
            return Task.CompletedTask;
        }
    }
}
