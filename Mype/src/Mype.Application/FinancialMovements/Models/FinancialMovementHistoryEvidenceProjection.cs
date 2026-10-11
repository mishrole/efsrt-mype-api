using System;
using Mype.Domain.MovementEvidences;

namespace Mype.Application.FinancialMovements.Models
{
    public sealed record FinancialMovementHistoryEvidenceProjection(
        Guid Id,
        string OriginalFileName,
        string MimeType,
        long SizeBytes,
        MovementEvidenceStatus Status,
        DateTimeOffset UploadedAt,
        uint Version
    );
}
