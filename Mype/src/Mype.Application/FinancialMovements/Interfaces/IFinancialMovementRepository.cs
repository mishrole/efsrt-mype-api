using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mype.Application.FinancialMovements.Models;
using Mype.Domain.FinancialMovements;

namespace Mype.Application.FinancialMovements.Interfaces
{
    public interface IFinancialMovementRepository
    {
        Task AddAsync(FinancialMovement movement, CancellationToken cancellationToken);
        Task<FinancialMovement> GetCurrentOpeningBalanceTrackedAsync(
            Guid businessId,
            CancellationToken cancellationToken
        );
        Task<FinancialMovementHistoryDetailProjection> GetCurrentOpeningBalanceAsync(
            Guid businessId,
            CancellationToken cancellationToken
        );
        Task<FinancialMovement> GetTrackedByIdAndBusinessAsync(
            Guid movementId,
            Guid businessId,
            CancellationToken cancellationToken
        );

        Task<FinancialMovement> GetAggregateTrackedByIdAndBusinessAsync(
            Guid movementId,
            Guid businessId,
            CancellationToken cancellationToken
        );

        void SetOriginalVersion(FinancialMovement movement, uint version);

        void SetOriginalVersions(
            FinancialMovement movement,
            uint movementVersion,
            FinancialMovementItem item,
            uint itemVersion
        );

        void SetOriginalVersions(
            FinancialMovement movement,
            uint movementVersion,
            IReadOnlyDictionary<Guid, uint> itemVersions
        );

        Task<FinancialMovementDraftProjection> GetDraftByIdAndBusinessAsync(
            Guid movementId,
            Guid businessId,
            CancellationToken cancellationToken
        );

        Task<
            IReadOnlyCollection<FinancialMovementDraftListItemProjection>
        > ListDraftsByBusinessAsync(Guid businessId, CancellationToken cancellationToken);
        Task<FinancialMovementHistoryPageProjection> SearchHistoryByBusinessAsync(
            Guid businessId,
            FinancialMovementHistoryFilters filters,
            CancellationToken cancellationToken
        );
        Task<FinancialMovementHistoryDetailProjection> GetHistoryDetailByIdAndBusinessAsync(
            Guid movementId,
            Guid businessId,
            CancellationToken cancellationToken
        );
    }
}
