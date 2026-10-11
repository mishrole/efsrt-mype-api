using System;
using System.Collections.Generic;
using Mype.Domain.FinancialMovements;

namespace Mype.Application.FinancialMovements.Commands.DiscardFinancialMovement
{
    public sealed record DiscardFinancialMovementResult(
        Guid Id,
        Guid BusinessId,
        FinancialMovementType Type,
        FinancialMovementStatus Status,
        DateOnly MovementDate,
        decimal TotalAmount,
        DateTimeOffset DiscardedAt,
        uint Version,
        IReadOnlyCollection<DiscardedEvidenceResult> Evidences
    );
}
