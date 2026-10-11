using System;
using System.Threading;
using System.Threading.Tasks;
using Mype.Application.Common.Models;

namespace Mype.Application.Common.Interfaces
{
    public interface IObjectStorage
    {
        Task<StoredObjectMetadata> UploadAsync(
            StorageUpload upload,
            CancellationToken cancellationToken
        );

        Task<TemporaryObjectAccessResult> CreateTemporaryReadAccessAsync(
            string objectKey,
            TimeSpan lifetime,
            CancellationToken cancellationToken
        );

        Task DeleteIfExistsAsync(string objectKey, CancellationToken cancellationToken);
    }
}
