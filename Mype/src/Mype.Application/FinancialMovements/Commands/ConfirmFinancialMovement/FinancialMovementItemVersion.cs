using System;

namespace Mype.Application.FinancialMovements.Commands.ConfirmFinancialMovement
{
    public sealed record FinancialMovementItemVersion(Guid ItemId, uint Version);
}
