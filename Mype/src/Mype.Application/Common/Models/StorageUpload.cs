using System.IO;

namespace Mype.Application.Common.Models
{
    public sealed record StorageUpload(
        string ObjectKey,
        string ContentType,
        long Length,
        Stream Content
    );
}
