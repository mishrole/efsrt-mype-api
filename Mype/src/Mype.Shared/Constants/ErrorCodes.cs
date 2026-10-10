namespace Mype.Shared.Constants
{
    public static class ErrorCodes
    {
        #region Common

        public const string InternalError = "INTERNAL_ERROR";
        public const string ValidationError = "VALIDATION_ERROR";
        public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";

        #endregion

        #region Auth

        public const string EmailAlreadyRegistered = "EMAIL_ALREADY_REGISTERED";

        public const string InvalidCredentials = "INVALID_CREDENTIALS";

        public const string AccountUnavailable = "ACCOUNT_UNAVAILABLE";

        public const string Unauthenticated = "UNAUTHENTICATED";

        #endregion

        #region Businesses

        public const string UnsupportedCurrency = "UNSUPPORTED_CURRENCY";

        public const string SystemRoleUnavailable = "SYSTEM_ROLE_UNAVAILABLE";

        public const string BusinessCreationFailed = "BUSINESS_CREATION_FAILED";

        public const string BusinessAccessForbidden = "BUSINESS_ACCESS_FORBIDDEN";

        public const string BusinessUnavailable = "BUSINESS_UNAVAILABLE";

        public const string RucAlreadyRegistered = "RUC_ALREADY_REGISTERED";

        #endregion

        #region Categories

        public const string CategoryAccessForbidden = "CATEGORY_ACCESS_FORBIDDEN";

        #endregion

        #region Products

        public const string ProductAccessForbidden = "PRODUCT_ACCESS_FORBIDDEN";

        public const string ProductAlreadyExists = "PRODUCT_ALREADY_EXISTS";

        public const string ProductCreationFailed = "PRODUCT_CREATION_FAILED";

        public const string ProductUpdateFailed = "PRODUCT_UPDATE_FAILED";

        public const string ProductStatusChangeFailed = "PRODUCT_STATUS_CHANGE_FAILED";

        public const string ProductNotFound = "PRODUCT_NOT_FOUND";

        public const string CategoryNotFound = "CATEGORY_NOT_FOUND";

        public const string CategoryUnavailable = "CATEGORY_UNAVAILABLE";

        public const string ProductCategoryMustBeSale = "PRODUCT_CATEGORY_MUST_BE_SALE";

        public const string ProductCategoryUnavailable = "PRODUCT_CATEGORY_UNAVAILABLE";

        public const string ProductAlreadyInactive = "PRODUCT_ALREADY_INACTIVE";

        public const string ProductAlreadyActive = "PRODUCT_ALREADY_ACTIVE";

        #endregion

        #region FinancialMovements

        public const string MovementAccessForbidden = "MOVEMENT_ACCESS_FORBIDDEN";
        public const string MovementNotFound = "MOVEMENT_NOT_FOUND";
        public const string MovementNotEditable = "MOVEMENT_NOT_EDITABLE";
        public const string MovementCreationFailed = "MOVEMENT_CREATION_FAILED";
        public const string MovementUpdateFailed = "MOVEMENT_UPDATE_FAILED";
        public const string MovementItemNotFound = "MOVEMENT_ITEM_NOT_FOUND";
        public const string MovementItemAlreadyRetired = "MOVEMENT_ITEM_ALREADY_RETIRED";
        public const string MovementMustBeSale = "MOVEMENT_MUST_BE_SALE";
        public const string MovementMustBeExpense = "MOVEMENT_MUST_BE_EXPENSE";
        public const string ExpenseCategoryUnavailable = "EXPENSE_CATEGORY_UNAVAILABLE";
        public const string ExpenseItemOperationFailed = "EXPENSE_ITEM_OPERATION_FAILED";
        public const string ProductInactive = "PRODUCT_INACTIVE";
        public const string MovementItemOperationFailed = "MOVEMENT_ITEM_OPERATION_FAILED";

        #endregion
    }
}
