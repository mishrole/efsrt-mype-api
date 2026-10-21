using System;
using MediatR;
using Mype.Application.Common;

namespace Mype.Application.FinancialMovements.Queries.ListFinancialMovementDraftItems
{
    public sealed class ListFinancialMovementDraftItemsQuery
        : IRequest<Result<FinancialMovementDraftItemsResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid CurrentUserId { get; set; }
    }
}
