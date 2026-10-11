using System.IO;

namespace Mype.Application.MovementEvidences.Models
{
    public sealed record EvidenceFile(
        string OriginalFileName,
        string DeclaredContentType,
        long Length,
        Stream Content
    );
}
