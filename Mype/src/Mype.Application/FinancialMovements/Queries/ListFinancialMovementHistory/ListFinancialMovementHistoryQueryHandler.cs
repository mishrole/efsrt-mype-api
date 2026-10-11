using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Common;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.FinancialMovements.Models;
using Mype.Application.FinancialMovements.Queries.Common;
using Mype.Application.Permissions.Interfaces;

namespace Mype.Application.FinancialMovements.Queries.ListFinancialMovementHistory
{
    public sealed class ListFinancialMovementHistoryQueryHandler
        : IRequestHandler<
            ListFinancialMovementHistoryQuery,
            Result<FinancialMovementHistoryPageResult>
        >
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly ICategoryRepository _categories;
        private readonly IFinancialMovementRepository _movements;

        public ListFinancialMovementHistoryQueryHandler(
            IBusinessMembershipRepository memberships,
            IPermissionRepository permissions,
            ICategoryRepository categories,
            IFinancialMovementRepository movements
        )
        {
            _memberships = memberships;
            _permissions = permissions;
            _categories = categories;
            _movements = movements;
        }

        public async Task<Result<FinancialMovementHistoryPageResult>> Handle(
            ListFinancialMovementHistoryQuery request,
            CancellationToken cancellationToken
        )
        {
            var accessError = await FinancialHistoryAuthorization.AuthorizeAsync(
                _memberships,
                _permissions,
                request.BusinessId,
                request.CurrentUserId,
                ListFinancialMovementHistoryErrors.BusinessAccessForbidden,
                ListFinancialMovementHistoryErrors.MovementAccessForbidden,
                cancellationToken
            );
            if (accessError != null)
                return Result<FinancialMovementHistoryPageResult>.Failure(accessError);

            if (
                request.CategoryId.HasValue
                && !await _categories.ExistsByIdAndBusinessAsync(
                    request.CategoryId.Value,
                    request.BusinessId,
                    cancellationToken
                )
            )
                return Result<FinancialMovementHistoryPageResult>.Failure(
                    ListFinancialMovementHistoryErrors.CategoryNotFound
                );

            try
            {
                var page = await _movements.SearchHistoryByBusinessAsync(
                    request.BusinessId,
                    new(
                        request.Type,
                        request.Status,
                        request.DateFrom,
                        request.DateTo,
                        request.CategoryId,
                        request.Page,
                        request.PageSize
                    ),
                    cancellationToken
                );
                var items = page
                    .Items.Select(item => new FinancialMovementHistoryItemResult(
                        item.Id,
                        item.Type,
                        item.Status,
                        item.MovementDate,
                        item.Description,
                        item.CurrencyCode,
                        item.TotalAmount,
                        item.ActiveItemCount,
                        item.HasActiveEvidence,
                        item.ConfirmedAt,
                        item.CancelledAt,
                        item.DiscardedAt
                    ))
                    .ToArray();
                return Result<FinancialMovementHistoryPageResult>.Success(
                    new(
                        items,
                        page.Page,
                        page.PageSize,
                        page.TotalCount,
                        page.Page > 1,
                        (long)page.Page * page.PageSize < page.TotalCount
                    )
                );
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return Result<FinancialMovementHistoryPageResult>.Failure(
                    ListFinancialMovementHistoryErrors.QueryFailed
                );
            }
        }
    }
}
