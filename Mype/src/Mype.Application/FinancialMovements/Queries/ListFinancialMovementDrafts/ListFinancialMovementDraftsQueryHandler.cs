using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Permissions.Constants;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.FinancialMovements.Queries.ListFinancialMovementDrafts
{
    public sealed class ListFinancialMovementDraftsQueryHandler : IRequestHandler<ListFinancialMovementDraftsQuery, Result<IReadOnlyCollection<FinancialMovementDraftListItemResult>>>
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        public ListFinancialMovementDraftsQueryHandler(IBusinessMembershipRepository memberships, IPermissionRepository permissions, IFinancialMovementRepository movements)
        { _memberships = memberships; _permissions = permissions; _movements = movements; }

        public async Task<Result<IReadOnlyCollection<FinancialMovementDraftListItemResult>>> Handle(ListFinancialMovementDraftsQuery request, CancellationToken cancellationToken)
        {
            var context = await _memberships.GetContextByBusinessAndUserAsync(request.BusinessId, request.CurrentUserId, cancellationToken);
            if (context == null || context.MembershipStatus != BusinessMembershipStatus.Active || !context.RoleIsActive)
                return Failure(ListFinancialMovementDraftsErrors.BusinessAccessForbidden);
            if (context.BusinessStatus != BusinessStatus.Active) return Failure(ListFinancialMovementDraftsErrors.BusinessUnavailable);
            var permissions = await _permissions.ListActiveCodesByRoleIdAsync(context.RoleId, cancellationToken);
            if (!permissions.Contains(SystemPermissions.MovementRead.Code)) return Failure(ListFinancialMovementDraftsErrors.MovementAccessForbidden);
            var movements = await _movements.ListDraftsByBusinessAsync(request.BusinessId, cancellationToken);
            IReadOnlyCollection<FinancialMovementDraftListItemResult> result = movements.Select(m => new FinancialMovementDraftListItemResult(m.Id, m.Type, m.MovementDate, m.Description, m.CurrencyCode, m.TotalAmount, m.UpdatedAt, m.Version)).ToArray();
            return Result<IReadOnlyCollection<FinancialMovementDraftListItemResult>>.Success(result);
        }
        private static Result<IReadOnlyCollection<FinancialMovementDraftListItemResult>> Failure(ApplicationError error) => Result<IReadOnlyCollection<FinancialMovementDraftListItemResult>>.Failure(error);
    }
}
