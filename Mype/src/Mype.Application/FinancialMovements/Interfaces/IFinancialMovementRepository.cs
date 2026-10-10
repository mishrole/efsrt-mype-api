using Mype.Application.FinancialMovements.Models;
using Mype.Domain.FinancialMovements;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.FinancialMovements.Interfaces
{
    public interface IFinancialMovementRepository
    {
        Task AddAsync(FinancialMovement movement, CancellationToken cancellationToken);
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

        Task<FinancialMovementDraftProjection> GetDraftByIdAndBusinessAsync(
            Guid movementId,
            Guid businessId,
            CancellationToken cancellationToken
        );

        Task<IReadOnlyCollection<FinancialMovementDraftListItemProjection>> ListDraftsByBusinessAsync(
            Guid businessId,
            CancellationToken cancellationToken
        );
    }
}
