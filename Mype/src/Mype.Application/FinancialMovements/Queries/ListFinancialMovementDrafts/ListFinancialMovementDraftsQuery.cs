using System;
using System.Collections.Generic;
using MediatR;
using Mype.Application.Common;

namespace Mype.Application.FinancialMovements.Queries.ListFinancialMovementDrafts
{
    public sealed class ListFinancialMovementDraftsQuery
        : IRequest<Result<IReadOnlyCollection<FinancialMovementDraftListItemResult>>>
    {
        public Guid BusinessId { get; set; }
        public Guid CurrentUserId { get; set; }
    }
}
