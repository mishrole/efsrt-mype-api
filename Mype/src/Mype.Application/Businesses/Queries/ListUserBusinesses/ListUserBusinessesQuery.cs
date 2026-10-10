using System;
using System.Collections.Generic;
using MediatR;
using Mype.Application.Common;

namespace Mype.Application.Businesses.Queries.ListUserBusinesses
{
    public sealed class ListUserBusinessesQuery
        : IRequest<Result<IReadOnlyCollection<BusinessSummaryResult>>>
    {
        public Guid CurrentUserId { get; set; }
    }
}
