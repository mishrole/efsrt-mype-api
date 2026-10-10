using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.Common
{
    public static class FinancialMovementItemErrors
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

        public static readonly ApplicationError MovementMustBeSale = new(
            ErrorCodes.MovementMustBeSale,
            ErrorMessages.MovementMustBeSale,
            ApplicationErrorType.UnprocessableEntity
        );

        public static readonly ApplicationError MovementMustBeExpense = new(
            ErrorCodes.MovementMustBeExpense,
            ErrorMessages.MovementMustBeExpense,
            ApplicationErrorType.UnprocessableEntity
        );

        public static readonly ApplicationError CategoryNotFound = new(
            ErrorCodes.CategoryNotFound,
            ErrorMessages.CategoryNotFound,
            ApplicationErrorType.NotFound
        );

        public static readonly ApplicationError ExpenseCategoryUnavailable = new(
            ErrorCodes.ExpenseCategoryUnavailable,
            ErrorMessages.ExpenseCategoryUnavailable,
            ApplicationErrorType.UnprocessableEntity
        );

        public static readonly ApplicationError ExpenseItemOperationFailed = new(
            ErrorCodes.ExpenseItemOperationFailed,
            ErrorMessages.ExpenseItemOperationFailed,
            ApplicationErrorType.Internal
        );
        public static readonly ApplicationError ProductNotFound = new(
            ErrorCodes.ProductNotFound,
            ErrorMessages.ProductNotFound,
            ApplicationErrorType.NotFound
        );

        public static readonly ApplicationError ProductInactive = new(
            ErrorCodes.ProductInactive,
            ErrorMessages.ProductInactive,
            ApplicationErrorType.UnprocessableEntity
        );

        public static readonly ApplicationError ProductCategoryUnavailable = new(
            ErrorCodes.ProductCategoryUnavailable,
            ErrorMessages.ProductCategoryUnavailable,
            ApplicationErrorType.UnprocessableEntity
        );

        public static readonly ApplicationError ItemNotFound = new(
            ErrorCodes.MovementItemNotFound,
            ErrorMessages.MovementItemNotFound,
            ApplicationErrorType.NotFound
        );

        public static readonly ApplicationError ItemAlreadyRetired = new(
            ErrorCodes.MovementItemAlreadyRetired,
            ErrorMessages.MovementItemAlreadyRetired,
            ApplicationErrorType.Conflict
        );

        public static readonly ApplicationError ConcurrencyConflict = new(
            ErrorCodes.ConcurrencyConflict,
            ErrorMessages.MovementConcurrencyConflict,
            ApplicationErrorType.Conflict
        );

        public static readonly ApplicationError OperationFailed = new(
            ErrorCodes.MovementItemOperationFailed,
            ErrorMessages.MovementItemOperationFailed,
            ApplicationErrorType.Internal
        );
    }
}
