using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mype.Application.FinancialDashboard.Interfaces;
using Mype.Application.FinancialDashboard.Models;
using Mype.Domain.FinancialMovements;
using Mype.Infrastructure.Persistence;

namespace Mype.Infrastructure.FinancialDashboard.Repositories
{
    public sealed class FinancialDashboardRepository : IFinancialDashboardRepository
    {
        private readonly MypeDbContext _dbContext;

        public FinancialDashboardRepository(MypeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<FinancialDashboardProjection> GetAsync(
            Guid businessId,
            DateOnly dateTo,
            CancellationToken cancellationToken
        )
        {
            var opening = await _dbContext
                .FinancialMovements.AsNoTracking()
                .Where(movement =>
                    movement.BusinessId == businessId
                    && movement.Type == FinancialMovementType.OpeningBalance
                    && movement.IsCurrentOpeningBalance
                )
                .Select(movement => new { movement.TotalAmount, movement.MovementDate })
                .SingleOrDefaultAsync(cancellationToken);
            if (opening == null)
                return null;
            var movements = await _dbContext
                .FinancialMovements.AsNoTracking()
                .Where(movement =>
                    movement.BusinessId == businessId
                    && movement.Status == FinancialMovementStatus.Confirmed
                    && (
                        movement.Type == FinancialMovementType.Sale
                        || movement.Type == FinancialMovementType.Expense
                    )
                    && movement.MovementDate >= opening.MovementDate
                    && movement.MovementDate >= opening.MovementDate
                    && movement.MovementDate <= dateTo
                )
                .Select(movement => new FinancialDashboardMovementProjection(
                    movement.MovementDate,
                    (int)movement.Type,
                    movement.TotalAmount,
                    movement.Type == FinancialMovementType.Sale
                        ? movement
                            .Items.Where(item => item.IsActive)
                            .Sum(item => item.Quantity * item.UnitCostSnapshot.GetValueOrDefault())
                        : 0m
                ))
                .ToArrayAsync(cancellationToken);
            var categories = await (
                from item in _dbContext.FinancialMovementItems.AsNoTracking()
                join movement in _dbContext.FinancialMovements.AsNoTracking()
                    on new { item.MovementId, item.BusinessId } equals new
                    {
                        MovementId = movement.Id,
                        movement.BusinessId,
                    }
                join category in _dbContext.Categories.AsNoTracking()
                    on new { item.CategoryId, item.BusinessId } equals new
                    {
                        CategoryId = category.Id,
                        category.BusinessId,
                    }
                where
                    item.BusinessId == businessId
                    && item.IsActive
                    && movement.Status == FinancialMovementStatus.Confirmed
                    && (
                        movement.Type == FinancialMovementType.Sale
                        || movement.Type == FinancialMovementType.Expense
                    )
                    && movement.MovementDate <= dateTo
                select new FinancialDashboardCategoryProjection(
                    category.Id,
                    category.Name,
                    category.IsActive,
                    (int)movement.Type,
                    movement.MovementDate,
                    item.SubtotalAmount
                )
            ).ToArrayAsync(cancellationToken);
            return new FinancialDashboardProjection(
                opening.TotalAmount,
                opening.MovementDate,
                movements,
                categories
            );
        }
    }
}
