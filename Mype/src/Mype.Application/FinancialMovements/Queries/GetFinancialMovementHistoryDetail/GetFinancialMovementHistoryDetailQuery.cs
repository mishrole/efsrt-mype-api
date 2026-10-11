using System;
using MediatR;
using Mype.Application.Common;

namespace Mype.Application.FinancialMovements.Queries.GetFinancialMovementHistoryDetail
{
    public sealed class GetFinancialMovementHistoryDetailQuery
        : IRequest<Result<FinancialMovementHistoryDetailResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid CurrentUserId { get; set; }
    }
}
