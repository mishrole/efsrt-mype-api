using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.MovementEvidences.Common
{
    public static class MovementEvidenceErrors
    {
        public static readonly ApplicationError BusinessAccessForbidden = new(
            ErrorCodes.BusinessAccessForbidden,
            ErrorMessages.BusinessAccessForbidden,
            ApplicationErrorType.Forbidden
        );
        public static readonly ApplicationError BusinessUnavailable = new(
            ErrorCodes.BusinessUnavailable,
            ErrorMessages.BusinessUnavailable,
            ApplicationErrorType.Conflict
        );
        public static readonly ApplicationError MovementAccessForbidden = new(
            ErrorCodes.MovementAccessForbidden,
            ErrorMessages.MovementAccessForbidden,
            ApplicationErrorType.Forbidden
        );
        public static readonly ApplicationError MovementNotFound = new(
            ErrorCodes.MovementNotFound,
            ErrorMessages.MovementNotFound,
            ApplicationErrorType.NotFound
        );
        public static readonly ApplicationError MovementNotEditable = new(
            ErrorCodes.MovementNotEditable,
            ErrorMessages.MovementNotEditable,
            ApplicationErrorType.Conflict
        );
        public static readonly ApplicationError EvidenceNotFound = new(
            ErrorCodes.EvidenceNotFound,
            ErrorMessages.EvidenceNotFound,
            ApplicationErrorType.NotFound
        );
        public static readonly ApplicationError EvidenceNotActive = new(
            ErrorCodes.EvidenceNotActive,
            ErrorMessages.EvidenceNotActive,
            ApplicationErrorType.Conflict
        );
        public static readonly ApplicationError InvalidFile = new(
            ErrorCodes.InvalidEvidenceFile,
            ErrorMessages.InvalidEvidenceFile,
            ApplicationErrorType.Validation
        );
        public static readonly ApplicationError UnsupportedFormat = new(
            ErrorCodes.UnsupportedEvidenceFormat,
            ErrorMessages.UnsupportedEvidenceFormat,
            ApplicationErrorType.UnprocessableEntity
        );
        public static readonly ApplicationError InvalidSize = new(
            ErrorCodes.InvalidEvidenceSize,
            ErrorMessages.InvalidEvidenceSize,
            ApplicationErrorType.UnprocessableEntity
        );
        public static readonly ApplicationError ConcurrencyConflict = new(
            ErrorCodes.ConcurrencyConflict,
            ErrorMessages.ConcurrencyConflict,
            ApplicationErrorType.Conflict
        );
        public static readonly ApplicationError StorageError = new(
            ErrorCodes.EvidenceStorageError,
            ErrorMessages.EvidenceStorageError,
            ApplicationErrorType.External
        );
        public static readonly ApplicationError OperationFailed = new(
            ErrorCodes.EvidenceOperationFailed,
            ErrorMessages.EvidenceOperationFailed,
            ApplicationErrorType.Internal
        );
    }
}
