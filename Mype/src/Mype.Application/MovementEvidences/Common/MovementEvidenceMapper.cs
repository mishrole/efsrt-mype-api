using Mype.Application.MovementEvidences.Models;
using Mype.Domain.MovementEvidences;

namespace Mype.Application.MovementEvidences.Common
{
    internal static class MovementEvidenceMapper
    {
        internal static MovementEvidenceResult ToResult(MovementEvidence evidence) =>
            new(
                evidence.Id,
                evidence.MovementId,
                evidence.ReplacesEvidenceId,
                evidence.OriginalFileName,
                evidence.MimeType,
                evidence.SizeBytes,
                evidence.Status,
                evidence.RemovalReason,
                evidence.UploadedAt,
                evidence.MarkedForPurgeAt,
                evidence.PurgeAfter,
                evidence.PurgedAt,
                evidence.Version
            );
    }
}
