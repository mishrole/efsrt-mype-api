using System;
using System.Collections.Generic;
using Mype.Domain.FinancialMovements;

namespace Mype.Application.FinancialMovements.Queries.ListFinancialMovementDraftItems
{
    public sealed record FinancialMovementDraftItemsResult(
        Guid MovementId,
        FinancialMovementType MovementType,
        uint MovementVersion,
        IReadOnlyCollection<FinancialMovementDraftItemResult> Items
    );
}
