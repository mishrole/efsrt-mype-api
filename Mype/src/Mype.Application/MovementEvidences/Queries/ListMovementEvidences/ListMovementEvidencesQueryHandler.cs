using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.MovementEvidences.Common;
using Mype.Application.MovementEvidences.Interfaces;
using Mype.Application.MovementEvidences.Models;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Permissions.Constants;

namespace Mype.Application.MovementEvidences.Queries.ListMovementEvidences
{
    public sealed class ListMovementEvidencesQueryHandler
        : IRequestHandler<
            ListMovementEvidencesQuery,
            Result<IReadOnlyCollection<MovementEvidenceResult>>
        >
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        private readonly IMovementEvidenceRepository _evidences;

        public ListMovementEvidencesQueryHandler(
            IBusinessMembershipRepository memberships,
            IPermissionRepository permissions,
            IFinancialMovementRepository movements,
            IMovementEvidenceRepository evidences
        )
        {
            _memberships = memberships;
            _permissions = permissions;
            _movements = movements;
            _evidences = evidences;
        }

        public async Task<Result<IReadOnlyCollection<MovementEvidenceResult>>> Handle(
            ListMovementEvidencesQuery request,
            CancellationToken cancellationToken
        )
        {
            var accessError = await MovementEvidenceAuthorization.AuthorizeAsync(
                _memberships,
                _permissions,
                request.BusinessId,
                request.CurrentUserId,
                SystemPermissions.MovementRead.Code,
                cancellationToken
            );
            if (accessError != null)
                return Result<IReadOnlyCollection<MovementEvidenceResult>>.Failure(accessError);

            var movement = await _movements.GetTrackedByIdAndBusinessAsync(
                request.MovementId,
                request.BusinessId,
                cancellationToken
            );
            if (movement == null)
                return Result<IReadOnlyCollection<MovementEvidenceResult>>.Failure(
                    MovementEvidenceErrors.MovementNotFound
                );

            var evidences = await _evidences.ListActiveByMovementAndBusinessAsync(
                request.MovementId,
                request.BusinessId,
                cancellationToken
            );
            return Result<IReadOnlyCollection<MovementEvidenceResult>>.Success(
                evidences.Select(MovementEvidenceMapper.ToResult).ToArray()
            );
        }
    }
}
