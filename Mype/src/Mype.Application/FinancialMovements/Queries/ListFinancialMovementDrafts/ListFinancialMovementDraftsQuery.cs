using MediatR;
using Mype.Application.Common;
using System;
using System.Collections.Generic;

namespace Mype.Application.FinancialMovements.Queries.ListFinancialMovementDrafts
{
    public sealed class ListFinancialMovementDraftsQuery : IRequest<Result<IReadOnlyCollection<FinancialMovementDraftListItemResult>>>
    {
        public Guid BusinessId { get; set; }
        public Guid CurrentUserId { get; set; }
    }
}
