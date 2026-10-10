using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.UpdateFinancialMovementDraft
{
    public static class UpdateFinancialMovementDraftErrors
    {

        public static readonly ApplicationError BusinessAccessForbidden = new(ErrorCodes.BusinessAccessForbidden, ErrorMessages.BusinessAccessForbidden, ApplicationErrorType.Forbidden);
        public static readonly ApplicationError BusinessUnavailable = new(ErrorCodes.BusinessUnavailable, ErrorMessages.BusinessUnavailable, ApplicationErrorType.Conflict);
        public static readonly ApplicationError MovementAccessForbidden = new(ErrorCodes.MovementAccessForbidden, ErrorMessages.MovementAccessForbidden, ApplicationErrorType.Forbidden);

        public static readonly ApplicationError MovementNotFound = new(ErrorCodes.MovementNotFound, ErrorMessages.MovementNotFound, ApplicationErrorType.NotFound);
        public static readonly ApplicationError MovementNotEditable = new(ErrorCodes.MovementNotEditable, ErrorMessages.MovementNotEditable, ApplicationErrorType.Conflict);
        public static readonly ApplicationError MovementConcurrencyConflict = new(ErrorCodes.MovementConcurrencyConflict, ErrorMessages.MovementConcurrencyConflict, ApplicationErrorType.Conflict);
        public static readonly ApplicationError MovementUpdateFailed = new(ErrorCodes.MovementUpdateFailed, ErrorMessages.MovementUpdateFailed, ApplicationErrorType.Internal);
    }
}
