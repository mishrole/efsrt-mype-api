using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.FinancialMovements.Queries.Common;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;

namespace Mype.Application.FinancialMovements.Queries.GetFinancialMovementHistoryDetail
{
    public sealed class GetFinancialMovementHistoryDetailQueryHandler
        : IRequestHandler<
            GetFinancialMovementHistoryDetailQuery,
            Result<FinancialMovementHistoryDetailResult>
        >
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;

        public GetFinancialMovementHistoryDetailQueryHandler(
            IBusinessMembershipRepository memberships,
            IPermissionRepository permissions,
            IFinancialMovementRepository movements
        )
        {
            _memberships = memberships;
            _permissions = permissions;
            _movements = movements;
        }

        public async Task<Result<FinancialMovementHistoryDetailResult>> Handle(
            GetFinancialMovementHistoryDetailQuery request,
            CancellationToken cancellationToken
        )
        {
            var accessError = await FinancialHistoryAuthorization.AuthorizeAsync(
                _memberships,
                _permissions,
                request.BusinessId,
                request.CurrentUserId,
                GetFinancialMovementHistoryDetailErrors.BusinessAccessForbidden,
                GetFinancialMovementHistoryDetailErrors.MovementAccessForbidden,
                cancellationToken
            );
            if (accessError != null)
                return Result<FinancialMovementHistoryDetailResult>.Failure(accessError);

            try
            {
                var movement = await _movements.GetHistoryDetailByIdAndBusinessAsync(
                    request.MovementId,
                    request.BusinessId,
                    cancellationToken
                );
                if (movement == null)
                    return Result<FinancialMovementHistoryDetailResult>.Failure(
                        GetFinancialMovementHistoryDetailErrors.MovementNotFound
                    );

                var rolePermissions = await GetPermissionsAsync(request, cancellationToken);
                decimal? estimatedCost = null;
                decimal? estimatedMargin = null;
                if (movement.Type == FinancialMovementType.Sale)
                {
                    estimatedCost = movement
                        .Items.Where(item => item.IsActive)
                        .Sum(item =>
                            decimal.Round(
                                item.Quantity * item.UnitCostSnapshot.GetValueOrDefault(),
                                2,
                                MidpointRounding.AwayFromZero
                            )
                        );
                    estimatedMargin = movement.TotalAmount - estimatedCost;
                }

                var items = movement
                    .Items.Select(item => new FinancialMovementHistoryItemDetailResult(
                        item.Id,
                        item.CategoryId,
                        item.CategoryName,
                        item.CategoryIsActive,
                        item.ProductId,
                        item.ProductName,
                        item.ProductIsActive,
                        item.Description,
                        item.Quantity,
                        item.UnitAmount,
                        item.UnitCostSnapshot,
                        item.SubtotalAmount,
                        item.IsActive,
                        item.RetiredAt,
                        item.Version
                    ))
                    .ToArray();
                var evidences = movement
                    .Evidences.Select(evidence => new FinancialMovementHistoryEvidenceResult(
                        evidence.Id,
                        evidence.OriginalFileName,
                        evidence.MimeType,
                        evidence.SizeBytes,
                        evidence.Status,
                        evidence.UploadedAt,
                        evidence.Version
                    ))
                    .ToArray();
                return Result<FinancialMovementHistoryDetailResult>.Success(
                    new(
                        movement.Id,
                        movement.BusinessId,
                        movement.Type,
                        movement.Status,
                        movement.MovementDate,
                        movement.Description,
                        movement.CurrencyCode,
                        movement.TotalAmount,
                        movement.CreatedByUserId,
                        movement.UpdatedByUserId,
                        movement.ConfirmedByUserId,
                        movement.CancelledByUserId,
                        movement.CancellationReason,
                        movement.CreatedAt,
                        movement.UpdatedAt,
                        movement.ConfirmedAt,
                        movement.CancelledAt,
                        movement.DiscardedAt,
                        estimatedCost,
                        estimatedMargin,
                        movement.Status == FinancialMovementStatus.Draft,
                        GetAllowedActions(movement.Status, rolePermissions),
                        movement.Version,
                        items,
                        evidences
                    )
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return Result<FinancialMovementHistoryDetailResult>.Failure(
                    GetFinancialMovementHistoryDetailErrors.QueryFailed
                );
            }
        }

        private async Task<IReadOnlyCollection<string>> GetPermissionsAsync(
            GetFinancialMovementHistoryDetailQuery request,
            CancellationToken cancellationToken
        )
        {
            var context = await _memberships.GetContextByBusinessAndUserAsync(
                request.BusinessId,
                request.CurrentUserId,
                cancellationToken
            );
            return await _permissions.ListActiveCodesByRoleIdAsync(
                context.RoleId,
                cancellationToken
            );
        }

        private static IReadOnlyCollection<string> GetAllowedActions(
            FinancialMovementStatus status,
            IReadOnlyCollection<string> permissions
        )
        {
            var actions = new List<string>();
            if (status == FinancialMovementStatus.Draft)
            {
                if (permissions.Contains(SystemPermissions.MovementUpdate.Code))
                    actions.Add("UPDATE");
                if (permissions.Contains(SystemPermissions.MovementConfirm.Code))
                    actions.Add("CONFIRM");
                if (permissions.Contains(SystemPermissions.MovementDiscard.Code))
                    actions.Add("DISCARD");
            }
            else if (
                status == FinancialMovementStatus.Confirmed
                && permissions.Contains(SystemPermissions.MovementCancel.Code)
            )
                actions.Add("CANCEL");
            return actions;
        }
    }
}
