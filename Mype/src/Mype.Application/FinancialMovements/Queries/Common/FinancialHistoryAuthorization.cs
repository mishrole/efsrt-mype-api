using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Permissions.Constants;

namespace Mype.Application.FinancialMovements.Queries.Common
{
    internal static class FinancialHistoryAuthorization
    {
        internal static async Task<ApplicationError> AuthorizeAsync(
            IBusinessMembershipRepository memberships,
            IPermissionRepository permissions,
            System.Guid businessId,
            System.Guid userId,
            ApplicationError businessAccessForbidden,
            ApplicationError movementAccessForbidden,
            CancellationToken cancellationToken
        )
        {
            var context = await memberships.GetContextByBusinessAndUserAsync(
                businessId,
                userId,
                cancellationToken
            );
            if (
                context == null
                || context.MembershipStatus != BusinessMembershipStatus.Active
                || !context.RoleIsActive
            )
                return businessAccessForbidden;

            var rolePermissions = await permissions.ListActiveCodesByRoleIdAsync(
                context.RoleId,
                cancellationToken
            );
            return rolePermissions.Contains(SystemPermissions.HistoryRead.Code)
                ? null
                : movementAccessForbidden;
        }
    }
}
