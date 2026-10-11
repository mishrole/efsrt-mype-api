using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mype.Application.MovementEvidences.Interfaces;
using Mype.Domain.MovementEvidences;
using Mype.Infrastructure.Persistence;

namespace Mype.Infrastructure.MovementEvidences.Repositories
{
    public sealed class MovementEvidenceRepository : IMovementEvidenceRepository
    {
        private readonly MypeDbContext _dbContext;

        public MovementEvidenceRepository(MypeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task AddAsync(MovementEvidence evidence, CancellationToken cancellationToken) =>
            _dbContext.MovementEvidences.AddAsync(evidence, cancellationToken).AsTask();

        public Task<MovementEvidence> GetTrackedByIdMovementAndBusinessAsync(
            Guid evidenceId,
            Guid movementId,
            Guid businessId,
            CancellationToken cancellationToken
        ) =>
            _dbContext.MovementEvidences.SingleOrDefaultAsync(
                evidence =>
                    evidence.Id == evidenceId
                    && evidence.MovementId == movementId
                    && evidence.BusinessId == businessId,
                cancellationToken
            );

        public async Task<
            IReadOnlyCollection<MovementEvidence>
        > ListActiveByMovementAndBusinessAsync(
            Guid movementId,
            Guid businessId,
            CancellationToken cancellationToken
        ) =>
            await _dbContext
                .MovementEvidences.AsNoTracking()
                .Where(evidence =>
                    evidence.MovementId == movementId
                    && evidence.BusinessId == businessId
                    && evidence.Status == MovementEvidenceStatus.Active
                )
                .OrderByDescending(evidence => evidence.UploadedAt)
                .ToArrayAsync(cancellationToken);

        public void SetOriginalVersion(MovementEvidence evidence, uint version) =>
            _dbContext.Entry(evidence).Property(current => current.Version).OriginalValue = version;
    }
}
