using Mype.Application.Common;
using SharedErrors = Mype.Application.FinancialMovements.Commands.Common.FinancialMovementItemErrors;

namespace Mype.Application.FinancialMovements.Commands.UpdateExpenseItem
{
    public static class UpdateExpenseItemErrors
    {
        public static ApplicationError BusinessAccessForbidden =>
            SharedErrors.BusinessAccessForbidden;

        public static ApplicationError BusinessUnavailable => SharedErrors.BusinessUnavailable;

        public static ApplicationError MovementAccessForbidden =>
            SharedErrors.MovementAccessForbidden;

        public static ApplicationError MovementNotFound => SharedErrors.MovementNotFound;

        public static ApplicationError MovementNotEditable => SharedErrors.MovementNotEditable;

        public static ApplicationError MovementMustBeExpense => SharedErrors.MovementMustBeExpense;

        public static ApplicationError CategoryNotFound => SharedErrors.CategoryNotFound;

        public static ApplicationError ExpenseCategoryUnavailable =>
            SharedErrors.ExpenseCategoryUnavailable;

        public static ApplicationError ItemNotFound => SharedErrors.ItemNotFound;

        public static ApplicationError ItemAlreadyRetired => SharedErrors.ItemAlreadyRetired;

        public static ApplicationError ConcurrencyConflict => SharedErrors.ConcurrencyConflict;

        public static ApplicationError OperationFailed => SharedErrors.ExpenseItemOperationFailed;
    }
}
