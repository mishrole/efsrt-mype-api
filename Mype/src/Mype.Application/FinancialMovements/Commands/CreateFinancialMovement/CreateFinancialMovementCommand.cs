using MediatR;
using Mype.Application.Common;
using Mype.Domain.FinancialMovements;
using System;

namespace Mype.Application.FinancialMovements.Commands.CreateFinancialMovement
{
    public sealed class CreateFinancialMovementCommand : IRequest<Result<CreateFinancialMovementResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid CurrentUserId { get; set; }
        public FinancialMovementType Type { get; set; }
        public DateOnly MovementDate { get; set; }
        public string Description { get; set; }
    }
}
