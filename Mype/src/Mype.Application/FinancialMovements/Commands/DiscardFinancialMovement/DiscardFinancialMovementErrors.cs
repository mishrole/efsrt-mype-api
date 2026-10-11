using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Application.MovementEvidences.Common;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.DiscardFinancialMovement
{
    public static class DiscardFinancialMovementErrors
    {
        public static ApplicationError BusinessAccessForbidden =>
            MovementEvidenceErrors.BusinessAccessForbidden;
        public static ApplicationError BusinessUnavailable =>
            MovementEvidenceErrors.BusinessUnavailable;
        public static ApplicationError MovementAccessForbidden =>
            MovementEvidenceErrors.MovementAccessForbidden;
        public static ApplicationError MovementNotFound => MovementEvidenceErrors.MovementNotFound;
        public static ApplicationError MovementNotEditable =>
            MovementEvidenceErrors.MovementNotEditable;
        public static ApplicationError ConcurrencyConflict =>
            MovementEvidenceErrors.ConcurrencyConflict;

        public static readonly ApplicationError InvalidEvidenceVersions = new(
            ErrorCodes.ValidationError,
            ErrorMessages.InvalidDiscardEvidenceVersions,
            ApplicationErrorType.Validation
        );
        public static readonly ApplicationError OperationFailed = new(
            ErrorCodes.MovementDiscardFailed,
            ErrorMessages.MovementDiscardFailed,
            ApplicationErrorType.Internal
        );
    }
}
