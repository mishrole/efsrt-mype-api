namespace Mype.Shared.Constants
{
    public static class ErrorMessages
    {
        public const string VariableNotConfigured = "{0} no está configurado.";
        public const string VariableNotValid = "{0} no es válido.";
        public const string DataMigrationFailed = "Error crítico durante la migración de datos.";
        public const string StartupFailed = "Error crítico durante el inicio de la aplicación: {ExceptionType}";
        public const string VariableRequiredInProduction = "{0} es requerido en producción.";
        public const string DatabaseMigrationFailed = "Error crítico durante la migración de la base de datos.";
        public const string ExceptionHandlingMiddlewareError = "Ocurrió una excepción no controlada: {ExceptionMessage}";
        public const string UnexpectedError = "Ocurrió un error inesperado.";
        public const string SaveChangesError = "Ocurrió un error al guardar los cambios.";

        public const string InternalError = "Ocurrió un error interno.";
        public const string ValidationFailed = "Uno o más campos no son válidos.";

        #region Auth

        public const string EmailAlreadyRegistered = "El correo electrónico ya se encuentra registrado.";

        #endregion
    }
}
