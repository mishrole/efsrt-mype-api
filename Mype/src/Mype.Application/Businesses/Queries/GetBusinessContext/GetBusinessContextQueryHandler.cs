using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.Businesses.Queries.GetBusinessContext
{
    public class GetBusinessContextQueryHandler : IRequestHandler<GetBusinessContextQuery, Result<BusinessContextResult>>
    {
        private readonly
            IBusinessMembershipRepository
            _membershipRepository;

        private readonly IPermissionRepository
            _permissionRepository;

        public GetBusinessContextQueryHandler(
            IBusinessMembershipRepository
                membershipRepository,
            IPermissionRepository
                permissionRepository
        )
        {
            _membershipRepository =
                membershipRepository;

            _permissionRepository =
                permissionRepository;
        }

        public async Task<Result<BusinessContextResult>>
            Handle(
                GetBusinessContextQuery request,
                CancellationToken cancellationToken
            )
        {
            var context =
                await _membershipRepository
                    .GetContextByBusinessAndUserAsync(
                        request.BusinessId,
                        request.CurrentUserId,
                        cancellationToken
                    );

            if (context == null)
            {
                return Result<
                    BusinessContextResult
                >.Failure(
                    GetBusinessContextErrors
                        .BusinessAccessForbidden
                );
            }

            if (
                context.MembershipStatus !=
                BusinessMembershipStatus.Active
            )
            {
                return Result<
                    BusinessContextResult
                >.Failure(
                    GetBusinessContextErrors
                        .BusinessAccessForbidden
                );
            }

            if (!context.RoleIsActive)
            {
                return Result<
                    BusinessContextResult
                >.Failure(
                    GetBusinessContextErrors
                        .BusinessAccessForbidden
                );
            }

            if (
                context.BusinessStatus !=
                BusinessStatus.Active
            )
            {
                return Result<
                    BusinessContextResult
                >.Failure(
                    GetBusinessContextErrors
                        .BusinessUnavailable
                );
            }

            var permissions =
                await _permissionRepository
                    .ListActiveCodesByRoleIdAsync(
                        context.RoleId,
                        cancellationToken
                    );

            return Result<
                BusinessContextResult
            >.Success(
                new BusinessContextResult(
                    context.BusinessId,
                    context.DisplayName,
                    context.CurrencyCode,
                    context.MembershipId,
                    context.RoleId,
                    context.RoleCode,
                    permissions
                )
            );
        }
    }
}