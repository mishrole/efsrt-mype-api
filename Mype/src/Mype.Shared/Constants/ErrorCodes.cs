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
        public const string SessionUnavailable = "SESSION_UNAVAILABLE";
        public const string SessionReuseDetected = "SESSION_REUSE_DETECTED";
        public const string SessionRefreshFailed = "SESSION_REFRESH_FAILED";
        public const string SessionRevocationFailed = "SESSION_REVOCATION_FAILED";

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

        #region FinancialDashboard

        public const string DashboardAccessForbidden = "DASHBOARD_ACCESS_FORBIDDEN";
        public const string OpeningBalanceNotFound = "OPENING_BALANCE_NOT_FOUND";
        public const string FinancialDashboardQueryFailed = "FINANCIAL_DASHBOARD_QUERY_FAILED";

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
        public const string MovementRequiresActiveItems = "MOVEMENT_REQUIRES_ACTIVE_ITEMS";
        public const string InvalidMovementItems = "INVALID_MOVEMENT_ITEMS";
        public const string InvalidSaleItems = "INVALID_SALE_ITEMS";
        public const string InvalidExpenseItems = "INVALID_EXPENSE_ITEMS";
        public const string MovementConfirmationFailed = "MOVEMENT_CONFIRMATION_FAILED";
        public const string MovementDiscardFailed = "MOVEMENT_DISCARD_FAILED";
        public const string MovementCancellationFailed = "MOVEMENT_CANCELLATION_FAILED";
        public const string FinancialHistoryQueryFailed = "FINANCIAL_HISTORY_QUERY_FAILED";
        public const string MovementDetailQueryFailed = "MOVEMENT_DETAIL_QUERY_FAILED";
        public const string EvidenceNotFound = "EVIDENCE_NOT_FOUND";
        public const string EvidenceNotActive = "EVIDENCE_NOT_ACTIVE";
        public const string InvalidEvidenceFile = "INVALID_EVIDENCE_FILE";
        public const string UnsupportedEvidenceFormat = "UNSUPPORTED_EVIDENCE_FORMAT";
        public const string InvalidEvidenceSize = "INVALID_EVIDENCE_SIZE";
        public const string EvidenceStorageError = "EVIDENCE_STORAGE_ERROR";
        public const string EvidenceOperationFailed = "EVIDENCE_OPERATION_FAILED";

        #endregion
    }
}
