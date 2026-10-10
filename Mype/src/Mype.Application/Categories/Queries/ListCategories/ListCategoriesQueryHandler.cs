using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Common;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Permissions.Constants;

namespace Mype.Application.Categories.Queries.ListCategories
{
    public sealed class ListCategoriesQueryHandler
        : IRequestHandler<ListCategoriesQuery, Result<IReadOnlyCollection<CategoryListItemResult>>>
    {
        private readonly IBusinessMembershipRepository _membershipRepository;

        private readonly IPermissionRepository _permissionRepository;

        private readonly ICategoryRepository _categoryRepository;

        public ListCategoriesQueryHandler(
            IBusinessMembershipRepository membershipRepository,
            IPermissionRepository permissionRepository,
            ICategoryRepository categoryRepository
        )
        {
            _membershipRepository = membershipRepository;

            _permissionRepository = permissionRepository;

            _categoryRepository = categoryRepository;
        }

        public async Task<Result<IReadOnlyCollection<CategoryListItemResult>>> Handle(
            ListCategoriesQuery request,
            CancellationToken cancellationToken
        )
        {
            var context = await _membershipRepository.GetContextByBusinessAndUserAsync(
                request.BusinessId,
                request.CurrentUserId,
                cancellationToken
            );

            if (context == null)
            {
                return Failure(ListCategoriesErrors.BusinessAccessForbidden);
            }

            if (context.MembershipStatus != BusinessMembershipStatus.Active)
            {
                return Failure(ListCategoriesErrors.BusinessAccessForbidden);
            }

            if (!context.RoleIsActive)
            {
                return Failure(ListCategoriesErrors.BusinessAccessForbidden);
            }

            if (context.BusinessStatus != BusinessStatus.Active)
            {
                return Failure(ListCategoriesErrors.BusinessUnavailable);
            }

            var permissions = await _permissionRepository.ListActiveCodesByRoleIdAsync(
                context.RoleId,
                cancellationToken
            );

            if (!permissions.Contains(SystemPermissions.CategoryRead.Code))
            {
                return Failure(ListCategoriesErrors.CategoryAccessForbidden);
            }

            var categories = await _categoryRepository.ListByBusinessAsync(
                request.BusinessId,
                request.Type,
                request.IsActive,
                cancellationToken
            );

            IReadOnlyCollection<CategoryListItemResult> result = categories
                .Select(category => new CategoryListItemResult(
                    category.Id,
                    category.BusinessId,
                    category.Name,
                    category.Type,
                    category.IsDefault,
                    category.IsActive
                ))
                .ToArray();

            return Result<IReadOnlyCollection<CategoryListItemResult>>.Success(result);
        }

        private static Result<IReadOnlyCollection<CategoryListItemResult>> Failure(
            ApplicationError error
        )
        {
            return Result<IReadOnlyCollection<CategoryListItemResult>>.Failure(error);
        }
    }
}
