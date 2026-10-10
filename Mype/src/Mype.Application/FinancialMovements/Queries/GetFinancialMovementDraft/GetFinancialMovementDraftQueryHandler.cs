using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Permissions.Constants;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.FinancialMovements.Queries.GetFinancialMovementDraft
{
    public sealed class GetFinancialMovementDraftQueryHandler : IRequestHandler<GetFinancialMovementDraftQuery, Result<FinancialMovementDraftDetailResult>>
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        public GetFinancialMovementDraftQueryHandler(IBusinessMembershipRepository memberships, IPermissionRepository permissions, IFinancialMovementRepository movements)
        { _memberships = memberships; _permissions = permissions; _movements = movements; }

        public async Task<Result<FinancialMovementDraftDetailResult>> Handle(GetFinancialMovementDraftQuery request, CancellationToken cancellationToken)
        {
            var context = await _memberships.GetContextByBusinessAndUserAsync(request.BusinessId, request.CurrentUserId, cancellationToken);
            if (context == null || context.MembershipStatus != BusinessMembershipStatus.Active || !context.RoleIsActive)
                return Failure(GetFinancialMovementDraftErrors.BusinessAccessForbidden);
            if (context.BusinessStatus != BusinessStatus.Active) return Failure(GetFinancialMovementDraftErrors.BusinessUnavailable);
            var permissions = await _permissions.ListActiveCodesByRoleIdAsync(context.RoleId, cancellationToken);
            if (!permissions.Contains(SystemPermissions.MovementRead.Code)) return Failure(GetFinancialMovementDraftErrors.MovementAccessForbidden);
            var movement = await _movements.GetDraftByIdAndBusinessAsync(request.MovementId, request.BusinessId, cancellationToken);
            if (movement == null) return Failure(GetFinancialMovementDraftErrors.MovementNotFound);
            return Result<FinancialMovementDraftDetailResult>.Success(new(
                movement.Id, movement.BusinessId, movement.Type, movement.Status,
                movement.MovementDate, movement.Description, movement.CurrencyCode,
                movement.TotalAmount, movement.CreatedAt, movement.UpdatedAt, movement.Version
            ));
        }
        private static Result<FinancialMovementDraftDetailResult> Failure(ApplicationError error) => Result<FinancialMovementDraftDetailResult>.Failure(error);
    }
}
