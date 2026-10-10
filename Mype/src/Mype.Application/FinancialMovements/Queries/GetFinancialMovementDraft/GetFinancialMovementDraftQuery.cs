using MediatR;
using Mype.Application.Common;
using System;

namespace Mype.Application.FinancialMovements.Queries.GetFinancialMovementDraft
{
    public sealed class GetFinancialMovementDraftQuery : IRequest<Result<FinancialMovementDraftDetailResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid CurrentUserId { get; set; }
    }
}
