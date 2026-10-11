using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Application.FinancialMovements.Commands.Common;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.CancelFinancialMovement
{
    public static class CancelFinancialMovementErrors
    {
        public static ApplicationError BusinessAccessForbidden =>
            FinancialMovementItemErrors.BusinessAccessForbidden;
        public static ApplicationError BusinessUnavailable =>
            FinancialMovementItemErrors.BusinessUnavailable;
        public static ApplicationError MovementAccessForbidden =>
            FinancialMovementItemErrors.MovementAccessForbidden;
        public static ApplicationError MovementNotFound =>
            FinancialMovementItemErrors.MovementNotFound;
        public static ApplicationError MovementNotEditable =>
            FinancialMovementItemErrors.MovementNotEditable;
        public static ApplicationError ConcurrencyConflict =>
            FinancialMovementItemErrors.ConcurrencyConflict;
        public static readonly ApplicationError OperationFailed = new(
            ErrorCodes.MovementCancellationFailed,
            ErrorMessages.MovementCancellationFailed,
            ApplicationErrorType.Internal
        );
    }
}
