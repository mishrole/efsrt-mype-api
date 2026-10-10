using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mype.Application.BusinessMemberships.Models;
using Mype.Domain.BusinessMemberships;

namespace Mype.Application.BusinessMemberships.Interfaces
{
    public interface IBusinessMembershipRepository
    {
        Task AddAsync(BusinessMembership membership, CancellationToken cancellationToken);

        Task<IReadOnlyCollection<BusinessSummaryProjection>> ListActiveByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken
        );

        Task<BusinessContextProjection> GetContextByBusinessAndUserAsync(
            Guid businessId,
            Guid userId,
            CancellationToken cancellationToken
        );
    }
}
