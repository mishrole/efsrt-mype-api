namespace Mype.Application.Common.Models
{
    public sealed record StoredObjectMetadata(
        string ObjectKey,
        string ContentType,
        long SizeBytes,
        string ETag
    );
}
