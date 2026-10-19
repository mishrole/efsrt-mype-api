using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.ProductCategories;

namespace Mype.Application.ProductCategories
{
    internal static class ProductCategoryAccess
    {
        internal static async Task<ApplicationError> ValidateAsync(
            IBusinessMembershipRepository membershipRepository,
            IPermissionRepository permissionRepository,
            Guid businessId,
            Guid currentUserId,
            string requiredPermissionCode,
            CancellationToken cancellationToken
        )
        {
            var businessContext = await membershipRepository.GetContextByBusinessAndUserAsync(
                businessId,
                currentUserId,
                cancellationToken
            );

            if (
                businessContext == null
                || businessContext.MembershipStatus != BusinessMembershipStatus.Active
                || !businessContext.RoleIsActive
            )
            {
                return ProductCategoryErrors.BusinessAccessForbidden;
            }

            if (businessContext.BusinessStatus != BusinessStatus.Active)
            {
                return ProductCategoryErrors.BusinessUnavailable;
            }

            var activePermissionCodes = await permissionRepository.ListActiveCodesByRoleIdAsync(
                businessContext.RoleId,
                cancellationToken
            );

            if (!activePermissionCodes.Contains(requiredPermissionCode))
            {
                return ProductCategoryErrors.AccessForbidden;
            }

            return null;
        }

        internal static ProductCategoryResult Map(ProductCategory productCategory)
        {
            return new ProductCategoryResult(
                productCategory.Id,
                productCategory.BusinessId,
                productCategory.Name,
                productCategory.Color,
                productCategory.IsDefault,
                productCategory.IsActive,
                productCategory.DeactivatedAt,
                productCategory.CreatedAt,
                productCategory.UpdatedAt,
                productCategory.Version
            );
        }
    }
}
