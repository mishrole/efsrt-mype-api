using MediatR;
using Mype.Application.Common;
using System;

namespace Mype.Application.Businesses.Queries.GetBusinessContext
{
    public sealed class GetBusinessContextQuery : IRequest<Result<BusinessContextResult>>
    {
        public Guid BusinessId { get; set; }

        public Guid CurrentUserId { get; set; }
    }
}