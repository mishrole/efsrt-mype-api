using System;

namespace Mype.Application.FinancialMovements.Commands.DiscardFinancialMovement
{
    public sealed record MovementEvidenceVersion(Guid EvidenceId, uint Version);
}
