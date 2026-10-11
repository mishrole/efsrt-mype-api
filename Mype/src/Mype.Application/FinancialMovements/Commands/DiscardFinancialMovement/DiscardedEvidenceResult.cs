using System;
using Mype.Domain.MovementEvidences;

namespace Mype.Application.FinancialMovements.Commands.DiscardFinancialMovement
{
    public sealed record DiscardedEvidenceResult(
        Guid Id,
        MovementEvidenceStatus Status,
        EvidenceRemovalReason? RemovalReason,
        DateTimeOffset? MarkedForPurgeAt,
        DateTimeOffset? PurgeAfter,
        uint Version
    );
}
