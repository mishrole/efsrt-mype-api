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

        public const string Unauthenticated = "La autenticación es requerida o el token no es válido.";

        public const string EmailAlreadyRegistered = "El correo electrónico ya se encuentra registrado.";

        public const string InvalidCredentials = "El correo electrónico o la contraseña no son válidos.";

        public const string AccountUnavailable = "La cuenta no se encuentra disponible.";

        #endregion

        #region Businesses

        public const string UnsupportedCurrency = "La moneda seleccionada no se encuentra disponible.";

        public const string SystemRoleUnavailable = "El rol requerido por el sistema no se encuentra disponible.";

        public const string BusinessCreationFailed = "No fue posible crear el negocio.";

        public const string InvalidRuc = "El RUC no es válido.";

        public const string BusinessAccessForbidden = "No tiene acceso al negocio solicitado.";

        public const string BusinessUnavailable = "El negocio no se encuentra disponible.";

        public const string RucAlreadyRegistered = "El RUC ya se encuentra registrado.";

        #endregion

        #region Categories

        public const string CategoryAccessForbidden = "CATEGORY_ACCESS_FORBIDDEN";

        #endregion

        #region Products

        public const string ProductAccessForbidden =
            "No tiene permiso para realizar esta operación sobre los productos del negocio.";

        public const string ProductAlreadyExists =
            "Ya existe un producto con el mismo nombre en el negocio.";

        public const string ProductCreationFailed =
            "No fue posible crear el producto.";

        public const string ProductNotFound =
            "El producto solicitado no fue encontrado.";

        public const string CategoryNotFound =
            "La categoría solicitada no fue encontrada.";

        public const string CategoryUnavailable =
            "La categoría seleccionada no se encuentra disponible.";

        public const string
            ProductCategoryMustBeSale =
                "Los productos solo pueden utilizar categorías de venta.";

        #endregion


        #region Results

        public const string SuccessfulResultWithoutValue = "Un resultado satisfactorio debe contener un valor.";

        public const string FailedResultWithoutError = "Un resultado fallido debe contener un error.";

        #endregion


    }
}
