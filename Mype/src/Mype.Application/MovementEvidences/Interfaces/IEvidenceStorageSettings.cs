namespace Mype.Application.MovementEvidences.Interfaces
{
    public interface IEvidenceStorageSettings
    {
        string Provider { get; }
        string ContainerName { get; }
        long MaxSizeBytes { get; }
        int ReadUriMinutes { get; }
        int PurgeRetentionDays { get; }
    }
}
