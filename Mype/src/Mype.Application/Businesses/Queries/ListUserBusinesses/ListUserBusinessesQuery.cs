using MediatR;
using Mype.Application.Common;
using System;
using System.Collections.Generic;

namespace Mype.Application.Businesses.Queries.ListUserBusinesses
{
    public sealed class ListUserBusinessesQuery : IRequest<Result<IReadOnlyCollection<BusinessSummaryResult>>>
    {
        public Guid CurrentUserId { get; set; }
    }
}