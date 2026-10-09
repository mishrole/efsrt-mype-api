using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.Businesses.Queries.ListUserBusinesses
{
    public class ListUserBusinessesQueryHandler : IRequestHandler<ListUserBusinessesQuery, Result<IReadOnlyCollection<BusinessSummaryResult>>>
    {
        private readonly
            IBusinessMembershipRepository
            _membershipRepository;

        public ListUserBusinessesQueryHandler(
            IBusinessMembershipRepository
                membershipRepository
        )
        {
            _membershipRepository =
                membershipRepository;
        }

        public async Task<Result<IReadOnlyCollection<BusinessSummaryResult>>> Handle(
            ListUserBusinessesQuery request,
            CancellationToken cancellationToken
        )
        {
            var businesses =
                await _membershipRepository
                    .ListActiveByUserIdAsync(
                        request.CurrentUserId,
                        cancellationToken
                    );

            var result = businesses
                .Select(business =>
                    new BusinessSummaryResult(
                        business.BusinessId,
                        business.DisplayName,
                        business.CurrencyCode,
                        business.Status,
                        business.MembershipId,
                        business.RoleCode
                    )
                )
                .ToArray();

            return Result<
                IReadOnlyCollection<
                    BusinessSummaryResult
                >
            >.Success(result);
        }
    }
}