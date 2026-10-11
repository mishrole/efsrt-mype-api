namespace Mype.Infrastructure.Storage
{
    public sealed class EvidenceStorageOptions
    {
        public string Provider { get; init; } = string.Empty;
        public string ServiceUri { get; init; } = string.Empty;
        public string ContainerName { get; init; } = string.Empty;
        public long MaxSizeBytes { get; init; }
        public int ReadUriMinutes { get; init; }
        public int PurgeRetentionDays { get; init; }
    }
}
