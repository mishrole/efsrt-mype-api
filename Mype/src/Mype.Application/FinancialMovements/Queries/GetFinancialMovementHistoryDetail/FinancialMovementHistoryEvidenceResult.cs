using System;
using Mype.Domain.MovementEvidences;

namespace Mype.Application.FinancialMovements.Queries.GetFinancialMovementHistoryDetail
{
    public sealed record FinancialMovementHistoryEvidenceResult(
        Guid Id,
        string OriginalFileName,
        string MimeType,
        long SizeBytes,
        MovementEvidenceStatus Status,
        DateTimeOffset UploadedAt,
        uint Version
    );
}
