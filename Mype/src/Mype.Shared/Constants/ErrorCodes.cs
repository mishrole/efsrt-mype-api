namespace Mype.Shared.Constants
{
    public static class ErrorCodes
    {
        public const string InternalError = "INTERNAL_ERROR";
        public const string ValidationError = "VALIDATION_ERROR";

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

        public const string
            CategoryAccessForbidden = "CATEGORY_ACCESS_FORBIDDEN";

        #endregion

        #region Products

        public const string ProductAccessForbidden = "PRODUCT_ACCESS_FORBIDDEN";

        public const string ProductAlreadyExists = "PRODUCT_ALREADY_EXISTS";

        public const string ProductCreationFailed = "PRODUCT_CREATION_FAILED";

        public const string ProductNotFound = "PRODUCT_NOT_FOUND";

        public const string CategoryNotFound = "CATEGORY_NOT_FOUND";

        public const string CategoryUnavailable = "CATEGORY_UNAVAILABLE";

        public const string ProductCategoryMustBeSale = "PRODUCT_CATEGORY_MUST_BE_SALE";

        #endregion

    }
}
