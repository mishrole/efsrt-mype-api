using MediatR;
using Mype.Application.Common;
using System;

namespace Mype.Application.FinancialMovements.Commands.UpdateFinancialMovementDraft
{
    public sealed class UpdateFinancialMovementDraftCommand : IRequest<Result<UpdateFinancialMovementDraftResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid CurrentUserId { get; set; }
        public DateOnly MovementDate { get; set; }
        public string Description { get; set; }
        public uint Version { get; set; }
    }
}
