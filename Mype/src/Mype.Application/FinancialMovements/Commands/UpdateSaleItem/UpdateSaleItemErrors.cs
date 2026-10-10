using Mype.Application.Common;
using SharedErrors = Mype.Application.FinancialMovements.Commands.Common.FinancialMovementItemErrors;

namespace Mype.Application.FinancialMovements.Commands.UpdateSaleItem
{
    public static class UpdateSaleItemErrors
    {
        public static ApplicationError BusinessAccessForbidden =>
            SharedErrors.BusinessAccessForbidden;
        public static ApplicationError BusinessUnavailable => SharedErrors.BusinessUnavailable;
        public static ApplicationError MovementAccessForbidden =>
            SharedErrors.MovementAccessForbidden;
        public static ApplicationError MovementNotFound => SharedErrors.MovementNotFound;
        public static ApplicationError MovementNotEditable => SharedErrors.MovementNotEditable;
        public static ApplicationError MovementMustBeSale => SharedErrors.MovementMustBeSale;
        public static ApplicationError ProductNotFound => SharedErrors.ProductNotFound;
        public static ApplicationError ProductInactive => SharedErrors.ProductInactive;
        public static ApplicationError ProductCategoryUnavailable =>
            SharedErrors.ProductCategoryUnavailable;
        public static ApplicationError ItemNotFound => SharedErrors.ItemNotFound;
        public static ApplicationError ItemAlreadyRetired => SharedErrors.ItemAlreadyRetired;
        public static ApplicationError ConcurrencyConflict => SharedErrors.ConcurrencyConflict;
        public static ApplicationError OperationFailed => SharedErrors.OperationFailed;
    }
}
