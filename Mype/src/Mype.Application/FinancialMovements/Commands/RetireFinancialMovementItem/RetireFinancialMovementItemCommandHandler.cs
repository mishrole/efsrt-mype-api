using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.FinancialMovements.Models;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.RetireFinancialMovementItem
{
    public sealed class RetireFinancialMovementItemCommandHandler
        : IRequestHandler<
            RetireFinancialMovementItemCommand,
            Result<RetiredFinancialMovementItemResult>
        >
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public RetireFinancialMovementItemCommandHandler(
            IBusinessMembershipRepository a,
            IPermissionRepository b,
            IFinancialMovementRepository c,
            IUnitOfWork d,
            IClock e
        )
        {
            _memberships = a;
            _permissions = b;
            _movements = c;
            _unitOfWork = d;
            _clock = e;
        }

        public async Task<Result<RetiredFinancialMovementItemResult>> Handle(
            RetireFinancialMovementItemCommand r,
            CancellationToken t
        )
        {
            var x = await _memberships.GetContextByBusinessAndUserAsync(
                r.BusinessId,
                r.CurrentUserId,
                t
            );
            if (
                x == null
                || x.MembershipStatus != BusinessMembershipStatus.Active
                || !x.RoleIsActive
            )
                return F(RetireFinancialMovementItemErrors.BusinessAccessForbidden);
            if (x.BusinessStatus != BusinessStatus.Active)
                return F(RetireFinancialMovementItemErrors.BusinessUnavailable);
            if (
                !(await _permissions.ListActiveCodesByRoleIdAsync(x.RoleId, t)).Contains(
                    SystemPermissions.MovementUpdate.Code
                )
            )
                return F(RetireFinancialMovementItemErrors.MovementAccessForbidden);
            var m = await _movements.GetAggregateTrackedByIdAndBusinessAsync(
                r.MovementId,
                r.BusinessId,
                t
            );
            if (m == null)
                return F(RetireFinancialMovementItemErrors.MovementNotFound);
            if (!m.IsEditable())
                return F(RetireFinancialMovementItemErrors.MovementNotEditable);
            var i = m.Items.SingleOrDefault(z => z.Id == r.ItemId);
            if (i == null)
                return F(RetireFinancialMovementItemErrors.ItemNotFound);
            if (!i.IsActive)
                return F(RetireFinancialMovementItemErrors.ItemAlreadyRetired);
            _movements.SetOriginalVersions(m, r.MovementVersion, i, r.ItemVersion);
            i = m.RetireItem(r.ItemId, r.CurrentUserId, _clock.UtcNow);
            try
            {
                await _unitOfWork.SaveChangesAsync(t);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ApplicationErrorException ex) when (ex.Code == ErrorCodes.ConcurrencyConflict)
            {
                return F(RetireFinancialMovementItemErrors.ConcurrencyConflict);
            }
            catch
            {
                return F(RetireFinancialMovementItemErrors.OperationFailed);
            }
            return Result<RetiredFinancialMovementItemResult>.Success(
                new(i.Id, i.IsActive, i.RetiredAt.Value, i.Version, m.TotalAmount, m.Version)
            );
        }

        private static Result<RetiredFinancialMovementItemResult> F(ApplicationError e) =>
            Result<RetiredFinancialMovementItemResult>.Failure(e);
    }
}
