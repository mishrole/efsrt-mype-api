namespace Mype.Shared.Constants
{
    public static class ErrorMessages
    {
        public const string VariableNotConfigured = "{0} is not configured.";
        public const string DataMigrationFailed = "Critical error during database migration.";
        public const string StartupFailed = "Critical error during application startup: {0}";
        public const string VariableRequiredInProduction = "{0} is required in production.";
        public const string DatabaseMigrationFailed = "Critical error during database migration: {0}";
        public const string ExceptionHandlingMiddlewareError = "An unhandled exception occurred: {0}";
        public const string UnexpectedError = "An unexpected error occurred.";
        public const string SaveChangesError = "An error occurred while saving changes.";

        public const string InternalError = "Ocurrió un error interno.";

        #region Auth

        public const string EmailAlreadyRegistered = "El correo electrónico ya se encuentra registrado.";

        #endregion
    }
}
