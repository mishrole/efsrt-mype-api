namespace Mype.Application.MovementEvidences.Models
{
    public sealed record ValidatedEvidenceFile(
        string OriginalFileName,
        string MimeType,
        string Extension,
        long Length
    );
}
