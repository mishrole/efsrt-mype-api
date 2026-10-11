using System;
using MediatR;
using Mype.Application.Common;
using Mype.Domain.FinancialMovements;

namespace Mype.Application.FinancialMovements.Queries.ListFinancialMovementHistory
{
    public sealed class ListFinancialMovementHistoryQuery
        : IRequest<Result<FinancialMovementHistoryPageResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid CurrentUserId { get; set; }
        public FinancialMovementType? Type { get; set; }
        public FinancialMovementStatus? Status { get; set; }
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public Guid? CategoryId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
