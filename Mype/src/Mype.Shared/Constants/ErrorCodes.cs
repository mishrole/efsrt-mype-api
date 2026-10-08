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

        #endregion

        #region Businesses

        public const string UnsupportedCurrency = "UNSUPPORTED_CURRENCY";

        public const string SystemRoleUnavailable = "SYSTEM_ROLE_UNAVAILABLE";

        public const string BusinessCreationFailed = "BUSINESS_CREATION_FAILED";

        #endregion
    }
}
