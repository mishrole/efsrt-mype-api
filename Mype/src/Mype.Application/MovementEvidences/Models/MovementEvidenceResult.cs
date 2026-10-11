using System;
using Mype.Domain.MovementEvidences;

namespace Mype.Application.MovementEvidences.Models
{
    public sealed record MovementEvidenceResult(
        Guid Id,
        Guid MovementId,
        Guid? ReplacesEvidenceId,
        string OriginalFileName,
        string MimeType,
        long SizeBytes,
        MovementEvidenceStatus Status,
        EvidenceRemovalReason? RemovalReason,
        DateTimeOffset UploadedAt,
        DateTimeOffset? MarkedForPurgeAt,
        DateTimeOffset? PurgeAfter,
        DateTimeOffset? PurgedAt,
        uint Version
    );

    public sealed record TemporaryEvidenceAccessResult(Uri Url, DateTimeOffset ExpiresAt);
}
