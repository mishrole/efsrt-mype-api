using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Permissions.Constants;

namespace Mype.Application.MovementEvidences.Common
{
    internal static class MovementEvidenceAuthorization
    {
        internal static async Task<ApplicationError> AuthorizeAsync(
            IBusinessMembershipRepository memberships,
            IPermissionRepository permissions,
            Guid businessId,
            Guid userId,
            string requiredPermission,
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
                return MovementEvidenceErrors.BusinessAccessForbidden;

            if (context.BusinessStatus != BusinessStatus.Active)
                return MovementEvidenceErrors.BusinessUnavailable;

            var codes = await permissions.ListActiveCodesByRoleIdAsync(
                context.RoleId,
                cancellationToken
            );

            return codes.Contains(requiredPermission)
                ? null
                : MovementEvidenceErrors.MovementAccessForbidden;
        }
    }
}
