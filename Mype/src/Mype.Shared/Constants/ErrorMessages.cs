namespace Mype.Shared.Constants
{
    public static class ErrorMessages
    {
        public const string VariableNotConfigured = "{0} no está configurado.";

        public const string VariableNotValid = "{0} no es válido.";

        public const string DataMigrationFailed = "Error crítico durante la migración de datos.";

        public const string StartupFailed =
            "Error crítico durante el inicio de la aplicación: {ExceptionType}";

        public const string VariableRequiredInProduction = "{0} es requerido en producción.";

        public const string DatabaseMigrationFailed =
            "Error crítico durante la migración de la base de datos.";

        public const string ExceptionHandlingMiddlewareError =
            "Ocurrió una excepción no controlada: {ExceptionMessage}";

        #region Common

        public const string UnexpectedError = "Ocurrió un error inesperado.";

        public const string SaveChangesError = "Ocurrió un error al guardar los cambios.";

        public const string InternalError = "Ocurrió un error interno.";

        public const string ValidationFailed = "Uno o más campos no son válidos.";

        public const string BadHttpRequestReceived = "La solicitud HTTP no pudo ser procesada.";
        public const string ConcurrencyConflict = "La información fue modificada por otro proceso.";

        #endregion

        #region Auth

        public const string Unauthenticated =
            "La autenticación es requerida o el token no es válido.";

        public const string EmailAlreadyRegistered =
            "El correo electrónico ya se encuentra registrado.";

        public const string InvalidCredentials =
            "El correo electrónico o la contraseña no son válidos.";

        public const string AccountUnavailable = "La cuenta no se encuentra disponible.";

        #endregion

        #region Businesses

        public const string UnsupportedCurrency =
            "La moneda seleccionada no se encuentra disponible.";

        public const string SystemRoleUnavailable =
            "El rol requerido por el sistema no se encuentra disponible.";

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

        public const string ProductCreationFailed = "No fue posible crear el producto.";

        public const string ProductUpdateFailed = "No fue posible actualizar el producto.";

        public const string ProductStatusChangeFailed =
            "No fue posible cambiar el estado del producto.";

        public const string ProductNotFound = "El producto solicitado no fue encontrado.";

        public const string CategoryNotFound = "La categoría solicitada no fue encontrada.";

        public const string CategoryUnavailable =
            "La categoría seleccionada no se encuentra disponible.";

        public const string ProductCategoryMustBeSale =
            "Los productos solo pueden utilizar categorías de venta.";

        public const string ProductCategoryUnavailable =
            "La categoría actual del producto no se encuentra disponible para reactivarlo.";

        public const string ProductConcurrencyConflict =
            "El producto fue modificado por otro proceso. Recargue los datos e inténtelo nuevamente.";

        public const string ProductAlreadyInactive = "El producto ya se encuentra inactivo.";

        public const string ProductAlreadyActive = "El producto ya se encuentra activo.";

        #endregion

        #region FinancialMovements

        public const string MovementAccessForbidden =
            "No tiene permiso para realizar esta operación sobre los movimientos del negocio.";
        public const string MovementNotFound = "El movimiento solicitado no fue encontrado.";
        public const string MovementNotEditable =
            "El movimiento ya no se encuentra disponible para edición.";
        public const string MovementConcurrencyConflict =
            "El movimiento fue modificado por otro proceso. Recargue los datos e inténtelo nuevamente.";
        public const string MovementCreationFailed = "No fue posible crear el movimiento.";
        public const string MovementUpdateFailed = "No fue posible actualizar el movimiento.";
        public const string MovementItemNotFound = "El í­tem solicitado no fue encontrado.";
        public const string MovementItemAlreadyRetired = "El í­tem ya se encuentra retirado.";
        public const string MovementMustBeSale = "El movimiento debe ser una venta en borrador.";
        public const string ProductInactive = "El producto no se encuentra disponible.";
        public const string MovementItemOperationFailed = "No fue posible procesar el í­tem del movimiento.";

        #endregion

        #region Results

        public const string SuccessfulResultWithoutValue =
            "Un resultado satisfactorio debe contener un valor.";

        public const string FailedResultWithoutError =
            "Un resultado fallido debe contener un error.";

        #endregion
    }
}


