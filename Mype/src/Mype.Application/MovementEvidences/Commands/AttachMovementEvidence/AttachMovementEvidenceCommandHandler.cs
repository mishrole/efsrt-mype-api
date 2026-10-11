using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.Common.Models;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.MovementEvidences.Common;
using Mype.Application.MovementEvidences.Interfaces;
using Mype.Application.MovementEvidences.Models;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.MovementEvidences;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Application.MovementEvidences.Commands.AttachMovementEvidence
{
    public sealed class AttachMovementEvidenceCommandHandler
        : IRequestHandler<AttachMovementEvidenceCommand, Result<MovementEvidenceResult>>
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        private readonly IMovementEvidenceRepository _evidences;
        private readonly IEvidenceFileValidator _validator;
        private readonly IStorageObjectKeyGenerator _keyGenerator;
        private readonly IEvidenceStorageSettings _settings;
        private readonly IObjectStorage _storage;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public AttachMovementEvidenceCommandHandler(
            IBusinessMembershipRepository memberships,
            IPermissionRepository permissions,
            IFinancialMovementRepository movements,
            IMovementEvidenceRepository evidences,
            IEvidenceFileValidator validator,
            IStorageObjectKeyGenerator keyGenerator,
            IEvidenceStorageSettings settings,
            IObjectStorage storage,
            IUnitOfWork unitOfWork,
            IClock clock
        )
        {
            _memberships = memberships;
            _permissions = permissions;
            _movements = movements;
            _evidences = evidences;
            _validator = validator;
            _keyGenerator = keyGenerator;
            _settings = settings;
            _storage = storage;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<Result<MovementEvidenceResult>> Handle(
            AttachMovementEvidenceCommand request,
            CancellationToken cancellationToken
        )
        {
            var accessError = await MovementEvidenceAuthorization.AuthorizeAsync(
                _memberships,
                _permissions,
                request.BusinessId,
                request.CurrentUserId,
                SystemPermissions.MovementUpdate.Code,
                cancellationToken
            );
            if (accessError != null)
                return Failure(accessError);

            var movement = await _movements.GetTrackedByIdAndBusinessAsync(
                request.MovementId,
                request.BusinessId,
                cancellationToken
            );
            if (movement == null)
                return Failure(MovementEvidenceErrors.MovementNotFound);
            if (!movement.IsEditable())
                return Failure(MovementEvidenceErrors.MovementNotEditable);

            var validation = _validator.Validate(request.File);
            if (!validation.IsSuccess)
                return Failure(validation.Error);

            var file = validation.Value;
            var evidenceId = Guid.NewGuid();
            var objectKey = _keyGenerator.Generate(
                request.BusinessId,
                request.MovementId,
                evidenceId,
                file.Extension
            );

            try
            {
                await _storage.UploadAsync(
                    new StorageUpload(objectKey, file.MimeType, file.Length, request.File.Content),
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                await CompensateUploadAsync(objectKey);
                throw;
            }
            catch
            {
                return Failure(MovementEvidenceErrors.StorageError);
            }

            var utcNow = _clock.UtcNow;
            var evidence = MovementEvidence.CreateActive(
                evidenceId,
                request.BusinessId,
                request.MovementId,
                null,
                _settings.Provider,
                _settings.ContainerName,
                objectKey,
                file.OriginalFileName,
                file.MimeType,
                file.Length,
                request.CurrentUserId,
                utcNow
            );

            _movements.SetOriginalVersion(movement, request.MovementVersion);
            movement.RegisterEvidenceChange(request.CurrentUserId, utcNow);
            await _evidences.AddAsync(evidence, cancellationToken);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await CompensateUploadAsync(objectKey);
                throw;
            }
            catch (ApplicationErrorException exception)
                when (exception.Code == ErrorCodes.ConcurrencyConflict)
            {
                await CompensateUploadAsync(objectKey);
                return Failure(MovementEvidenceErrors.ConcurrencyConflict);
            }
            catch
            {
                await CompensateUploadAsync(objectKey);
                return Failure(MovementEvidenceErrors.OperationFailed);
            }

            return Result<MovementEvidenceResult>.Success(
                MovementEvidenceMapper.ToResult(evidence)
            );
        }

        private async Task CompensateUploadAsync(string objectKey)
        {
            try
            {
                await _storage.DeleteIfExistsAsync(objectKey, CancellationToken.None);
            }
            catch
            {
                // TODO: Emitir telemetria cuando falle la eliminacion compensatoria del objeto.
            }
        }

        private static Result<MovementEvidenceResult> Failure(ApplicationError error) =>
            Result<MovementEvidenceResult>.Failure(error);
    }
}
