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
    }
}
