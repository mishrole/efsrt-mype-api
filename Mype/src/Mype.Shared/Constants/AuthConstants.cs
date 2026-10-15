namespace Mype.Shared.Constants
{
    public static class AuthConstants
    {
        public const string TokenType = "Bearer";
        public const string RefreshTokenCookieName = "__Secure-mype-refresh-token";
        public const string RefreshTokenCookiePath = "/api/v1/auth";
        public const string SessionIdClaimName = "sid";
        public const int RefreshTokenSizeBytes = 32;
        public const int RefreshTokenIdleExpirationDays = 7;
        public const int RefreshTokenAbsoluteExpirationDays = 30;
    }
}
