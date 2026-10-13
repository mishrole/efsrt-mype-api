using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.FinancialMovements.Models;
using Mype.Domain.FinancialMovements;
using Mype.Infrastructure.Persistence;

namespace Mype.Infrastructure.FinancialMovements.Repositories
{
    public class FinancialMovementRepository : IFinancialMovementRepository
    {
        private readonly MypeDbContext _dbContext;

        public FinancialMovementRepository(MypeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task AddAsync(FinancialMovement movement, CancellationToken cancellationToken) =>
            _dbContext.FinancialMovements.AddAsync(movement, cancellationToken).AsTask();

        public Task<FinancialMovement> GetCurrentOpeningBalanceTrackedAsync(
            Guid businessId,
            CancellationToken cancellationToken
        ) =>
            _dbContext.FinancialMovements.SingleOrDefaultAsync(
                movement =>
                    movement.BusinessId == businessId
                    && movement.Type == FinancialMovementType.OpeningBalance
                    && movement.IsCurrentOpeningBalance,
                cancellationToken
            );

        public Task<FinancialMovementHistoryDetailProjection> GetCurrentOpeningBalanceAsync(
            Guid businessId,
            CancellationToken cancellationToken
        ) =>
            _dbContext
                .FinancialMovements.AsNoTracking()
                .Where(movement =>
                    movement.BusinessId == businessId
                    && movement.Type == FinancialMovementType.OpeningBalance
                    && movement.IsCurrentOpeningBalance
                )
                .Select(movement => new FinancialMovementHistoryDetailProjection(
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
                    movement.Version,
                    Array.Empty<FinancialMovementHistoryItemDetailProjection>(),
                    Array.Empty<FinancialMovementHistoryEvidenceProjection>()
                ))
                .SingleOrDefaultAsync(cancellationToken);

        public Task<FinancialMovement> GetTrackedByIdAndBusinessAsync(
            Guid movementId,
            Guid businessId,
            CancellationToken cancellationToken
        ) =>
            _dbContext.FinancialMovements.SingleOrDefaultAsync(
                m => m.Id == movementId && m.BusinessId == businessId,
                cancellationToken
            );

        public void SetOriginalVersion(FinancialMovement movement, uint version) =>
            _dbContext.Entry(movement).Property(m => m.Version).OriginalValue = version;

        public Task<FinancialMovementDraftProjection> GetDraftByIdAndBusinessAsync(
            Guid movementId,
            Guid businessId,
            CancellationToken cancellationToken
        ) =>
            _dbContext
                .FinancialMovements.AsNoTracking()
                .Where(m =>
                    m.Id == movementId
                    && m.BusinessId == businessId
                    && m.Status == FinancialMovementStatus.Draft
                )
                .Select(m => new FinancialMovementDraftProjection(
                    m.Id,
                    m.BusinessId,
                    m.Type,
                    m.Status,
                    m.MovementDate,
                    m.Description,
                    m.CurrencyCode,
                    m.TotalAmount,
                    m.CreatedAt,
                    m.UpdatedAt,
                    m.Version
                ))
                .SingleOrDefaultAsync(cancellationToken);

        public async Task<
            IReadOnlyCollection<FinancialMovementDraftListItemProjection>
        > ListDraftsByBusinessAsync(Guid businessId, CancellationToken cancellationToken) =>
            await _dbContext
                .FinancialMovements.AsNoTracking()
                .Where(m => m.BusinessId == businessId && m.Status == FinancialMovementStatus.Draft)
                .OrderByDescending(m => m.UpdatedAt)
                .Select(m => new FinancialMovementDraftListItemProjection(
                    m.Id,
                    m.Type,
                    m.MovementDate,
                    m.Description,
                    m.CurrencyCode,
                    m.TotalAmount,
                    m.UpdatedAt,
                    m.Version
                ))
                .ToArrayAsync(cancellationToken);

        public Task<FinancialMovement> GetAggregateTrackedByIdAndBusinessAsync(
            Guid movementId,
            Guid businessId,
            CancellationToken cancellationToken
        ) =>
            _dbContext
                .FinancialMovements.Include(movement => movement.Items)
                .SingleOrDefaultAsync(
                    movement => movement.Id == movementId && movement.BusinessId == businessId,
                    cancellationToken
                );

        public void SetOriginalVersions(
            FinancialMovement movement,
            uint movementVersion,
            FinancialMovementItem item,
            uint itemVersion
        )
        {
            SetOriginalVersion(movement, movementVersion);

            _dbContext.Entry(item).Property(currentItem => currentItem.Version).OriginalValue =
                itemVersion;
        }

        public void SetOriginalVersions(
            FinancialMovement movement,
            uint movementVersion,
            IReadOnlyDictionary<Guid, uint> itemVersions
        )
        {
            SetOriginalVersion(movement, movementVersion);

            foreach (var item in movement.Items.Where(current => current.IsActive))
            {
                _dbContext.Entry(item).Property(current => current.Version).OriginalValue =
                    itemVersions[item.Id];
                _dbContext.Entry(item).Property(current => current.SubtotalAmount).IsModified =
                    true;
            }
        }

        public async Task<FinancialMovementHistoryPageProjection> SearchHistoryByBusinessAsync(
            Guid businessId,
            FinancialMovementHistoryFilters filters,
            CancellationToken cancellationToken
        )
        {
            var query = _dbContext
                .FinancialMovements.AsNoTracking()
                .Where(movement => movement.BusinessId == businessId);

            if (filters.Type.HasValue)
                query = query.Where(movement => movement.Type == filters.Type.Value);
            if (filters.Status.HasValue)
                query = query.Where(movement => movement.Status == filters.Status.Value);
            if (filters.DateFrom.HasValue)
                query = query.Where(movement => movement.MovementDate >= filters.DateFrom.Value);
            if (filters.DateTo.HasValue)
                query = query.Where(movement => movement.MovementDate <= filters.DateTo.Value);
            if (filters.CategoryId.HasValue)
                query = query.Where(movement =>
                    movement.Items.Any(item =>
                        item.IsActive && item.CategoryId == filters.CategoryId.Value
                    )
                );

            var totalCount = await query.LongCountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(movement => movement.MovementDate)
                .ThenByDescending(movement => movement.CreatedAt)
                .ThenByDescending(movement => movement.Id)
                .Skip((filters.Page - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(movement => new FinancialMovementHistoryItemProjection(
                    movement.Id,
                    movement.Type,
                    movement.Status,
                    movement.MovementDate,
                    movement.Description,
                    movement.CurrencyCode,
                    movement.TotalAmount,
                    movement.Items.Count(item => item.IsActive),
                    _dbContext.MovementEvidences.Any(evidence =>
                        evidence.MovementId == movement.Id
                        && evidence.BusinessId == businessId
                        && evidence.Status
                            == Mype.Domain.MovementEvidences.MovementEvidenceStatus.Active
                    ),
                    movement.CreatedAt,
                    movement.ConfirmedAt,
                    movement.CancelledAt,
                    movement.DiscardedAt
                ))
                .ToArrayAsync(cancellationToken);

            return new(items, filters.Page, filters.PageSize, totalCount);
        }

        public async Task<FinancialMovementHistoryDetailProjection> GetHistoryDetailByIdAndBusinessAsync(
            Guid movementId,
            Guid businessId,
            CancellationToken cancellationToken
        )
        {
            var movement = await _dbContext
                .FinancialMovements.AsNoTracking()
                .Where(current => current.Id == movementId && current.BusinessId == businessId)
                .Select(current => new
                {
                    current.Id,
                    current.BusinessId,
                    current.Type,
                    current.Status,
                    current.MovementDate,
                    current.Description,
                    current.CurrencyCode,
                    current.TotalAmount,
                    current.CreatedByUserId,
                    current.UpdatedByUserId,
                    current.ConfirmedByUserId,
                    current.CancelledByUserId,
                    current.CancellationReason,
                    current.CreatedAt,
                    current.UpdatedAt,
                    current.ConfirmedAt,
                    current.CancelledAt,
                    current.DiscardedAt,
                    current.Version,
                })
                .SingleOrDefaultAsync(cancellationToken);
            if (movement == null)
                return null;

            var items = await (
                from item in _dbContext.FinancialMovementItems.AsNoTracking()
                join category in _dbContext.Categories.AsNoTracking()
                    on new { item.CategoryId, item.BusinessId } equals new
                    {
                        CategoryId = category.Id,
                        category.BusinessId,
                    }
                join product in _dbContext.Products.AsNoTracking()
                    on item.ProductId equals (Guid?)product.Id
                    into products
                from product in products.DefaultIfEmpty()
                where item.MovementId == movementId && item.BusinessId == businessId
                orderby item.CreatedAt, item.Id
                select new FinancialMovementHistoryItemDetailProjection(
                    item.Id,
                    item.CategoryId,
                    category.Name,
                    category.IsActive,
                    item.ProductId,
                    product == null ? null : product.Name,
                    product == null ? null : product.IsActive,
                    item.Description,
                    item.Quantity,
                    item.UnitAmount,
                    item.UnitCostSnapshot,
                    item.SubtotalAmount,
                    item.IsActive,
                    item.RetiredAt,
                    item.Version
                )
            ).ToArrayAsync(cancellationToken);

            var evidences = await _dbContext
                .MovementEvidences.AsNoTracking()
                .Where(evidence =>
                    evidence.MovementId == movementId && evidence.BusinessId == businessId
                )
                .OrderBy(evidence => evidence.UploadedAt)
                .ThenBy(evidence => evidence.Id)
                .Select(evidence => new FinancialMovementHistoryEvidenceProjection(
                    evidence.Id,
                    evidence.OriginalFileName,
                    evidence.MimeType,
                    evidence.SizeBytes,
                    evidence.Status,
                    evidence.UploadedAt,
                    evidence.Version
                ))
                .ToArrayAsync(cancellationToken);

            return new(
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
                movement.Version,
                items,
                evidences
            );
        }
    }
}
