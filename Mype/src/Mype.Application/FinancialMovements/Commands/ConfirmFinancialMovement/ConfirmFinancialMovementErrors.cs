using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;
using SharedErrors = Mype.Application.FinancialMovements.Commands.Common.FinancialMovementItemErrors;

namespace Mype.Application.FinancialMovements.Commands.ConfirmFinancialMovement
{
    public static class ConfirmFinancialMovementErrors
    {
        public static ApplicationError BusinessAccessForbidden =>
            SharedErrors.BusinessAccessForbidden;
        public static ApplicationError BusinessUnavailable => SharedErrors.BusinessUnavailable;
        public static ApplicationError MovementAccessForbidden =>
            SharedErrors.MovementAccessForbidden;
        public static ApplicationError MovementNotFound => SharedErrors.MovementNotFound;
        public static ApplicationError MovementNotEditable => SharedErrors.MovementNotEditable;
        public static ApplicationError ConcurrencyConflict => SharedErrors.ConcurrencyConflict;

        public static readonly ApplicationError InvalidItemVersions = new(
            ErrorCodes.ValidationError,
            ErrorMessages.InvalidMovementItemVersions,
            ApplicationErrorType.Validation
        );
        public static readonly ApplicationError RequiresActiveItems = new(
            ErrorCodes.MovementRequiresActiveItems,
            ErrorMessages.MovementRequiresActiveItems,
            ApplicationErrorType.UnprocessableEntity
        );
        public static readonly ApplicationError InvalidMovementItems = new(
            ErrorCodes.InvalidMovementItems,
            ErrorMessages.InvalidMovementItems,
            ApplicationErrorType.UnprocessableEntity
        );
        public static readonly ApplicationError InvalidSaleItems = new(
            ErrorCodes.InvalidSaleItems,
            ErrorMessages.InvalidSaleItems,
            ApplicationErrorType.UnprocessableEntity
        );
        public static readonly ApplicationError InvalidExpenseItems = new(
            ErrorCodes.InvalidExpenseItems,
            ErrorMessages.InvalidExpenseItems,
            ApplicationErrorType.UnprocessableEntity
        );
        public static readonly ApplicationError OperationFailed = new(
            ErrorCodes.MovementConfirmationFailed,
            ErrorMessages.MovementConfirmationFailed,
            ApplicationErrorType.Internal
        );
    }
}
