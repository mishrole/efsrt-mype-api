using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.FinancialMovements.Models;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;

namespace Mype.Application.FinancialMovements.Queries.ListFinancialMovementDraftItems
{
    public sealed class ListFinancialMovementDraftItemsQueryHandler
        : IRequestHandler<
            ListFinancialMovementDraftItemsQuery,
            Result<FinancialMovementDraftItemsResult>
        >
    {
        private readonly IBusinessMembershipRepository _membershipRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IFinancialMovementRepository _financialMovementRepository;

        public ListFinancialMovementDraftItemsQueryHandler(
            IBusinessMembershipRepository membershipRepository,
            IPermissionRepository permissionRepository,
            IFinancialMovementRepository financialMovementRepository
        )
        {
            _membershipRepository = membershipRepository;
            _permissionRepository = permissionRepository;
            _financialMovementRepository = financialMovementRepository;
        }

        public async Task<Result<FinancialMovementDraftItemsResult>> Handle(
            ListFinancialMovementDraftItemsQuery request,
            CancellationToken cancellationToken
        )
        {
            var businessContext = await _membershipRepository.GetContextByBusinessAndUserAsync(
                request.BusinessId,
                request.CurrentUserId,
                cancellationToken
            );

            if (
                businessContext == null
                || businessContext.MembershipStatus != BusinessMembershipStatus.Active
                || !businessContext.RoleIsActive
            )
            {
                return Failure(ListFinancialMovementDraftItemsErrors.BusinessAccessForbidden);
            }

            if (businessContext.BusinessStatus != BusinessStatus.Active)
            {
                return Failure(ListFinancialMovementDraftItemsErrors.BusinessUnavailable);
            }

            var activePermissionCodes = await _permissionRepository.ListActiveCodesByRoleIdAsync(
                businessContext.RoleId,
                cancellationToken
            );

            if (!activePermissionCodes.Contains(SystemPermissions.MovementRead.Code))
            {
                return Failure(ListFinancialMovementDraftItemsErrors.MovementAccessForbidden);
            }

            var projection = await _financialMovementRepository.GetDraftItemsByIdAndBusinessAsync(
                request.MovementId,
                request.BusinessId,
                cancellationToken
            );

            if (projection == null)
            {
                return Failure(ListFinancialMovementDraftItemsErrors.MovementNotFound);
            }

            var itemResults = projection
                .Items.Select(item => MapItem(projection.MovementType, item))
                .ToArray();

            return Result<FinancialMovementDraftItemsResult>.Success(
                new FinancialMovementDraftItemsResult(
                    projection.MovementId,
                    projection.MovementType,
                    projection.MovementVersion,
                    itemResults
                )
            );
        }

        private static FinancialMovementDraftItemResult MapItem(
            FinancialMovementType movementType,
            FinancialMovementDraftItemProjection item
        )
        {
            var isSale = movementType == FinancialMovementType.Sale;
            var estimatedCost = isSale
                ? RoundAmount(item.Quantity * item.UnitCostSnapshot.GetValueOrDefault())
                : (decimal?)null;
            var estimatedMargin = isSale ? item.SubtotalAmount - estimatedCost : null;

            return new FinancialMovementDraftItemResult(
                item.Id,
                item.CategoryId,
                item.CategoryName,
                item.ProductId,
                item.ProductCategoryId,
                isSale ? item.Description : null,
                isSale ? null : item.Description,
                item.Quantity,
                item.UnitAmount,
                item.UnitCostSnapshot,
                item.SubtotalAmount,
                estimatedCost,
                estimatedMargin,
                item.Version
            );
        }

        private static decimal RoundAmount(decimal amount)
        {
            return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        }

        private static Result<FinancialMovementDraftItemsResult> Failure(ApplicationError error)
        {
            return Result<FinancialMovementDraftItemsResult>.Failure(error);
        }
    }
}
