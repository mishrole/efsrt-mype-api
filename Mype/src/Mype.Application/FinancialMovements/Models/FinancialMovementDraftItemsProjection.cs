using System;
using System.Collections.Generic;
using Mype.Domain.FinancialMovements;

namespace Mype.Application.FinancialMovements.Models
{
    public sealed record FinancialMovementDraftItemsProjection(
        Guid MovementId,
        FinancialMovementType MovementType,
        uint MovementVersion,
        IReadOnlyCollection<FinancialMovementDraftItemProjection> Items
    );
}
