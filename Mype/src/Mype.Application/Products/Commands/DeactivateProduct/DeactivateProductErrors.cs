using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.Products.Commands.DeactivateProduct
{
    public static class DeactivateProductErrors
    {
        public static readonly ApplicationError BusinessAccessForbidden = new(
            ErrorCodes.BusinessAccessForbidden,
            ErrorMessages.BusinessAccessForbidden,
            ApplicationErrorType.Forbidden
        );
        public static readonly ApplicationError BusinessUnavailable = new(
            ErrorCodes.BusinessUnavailable,
            ErrorMessages.BusinessUnavailable,
            ApplicationErrorType.Conflict
        );
        public static readonly ApplicationError ProductAccessForbidden = new(
            ErrorCodes.ProductAccessForbidden,
            ErrorMessages.ProductAccessForbidden,
            ApplicationErrorType.Forbidden
        );
        public static readonly ApplicationError ProductNotFound = new(
            ErrorCodes.ProductNotFound,
            ErrorMessages.ProductNotFound,
            ApplicationErrorType.NotFound
        );
        public static readonly ApplicationError ProductConcurrencyConflict = new(
            ErrorCodes.ConcurrencyConflict,
            ErrorMessages.ProductConcurrencyConflict,
            ApplicationErrorType.Conflict
        );

        public static readonly ApplicationError ProductAlreadyInactive = new(
            ErrorCodes.ProductAlreadyInactive,
            ErrorMessages.ProductAlreadyInactive,
            ApplicationErrorType.Conflict
        );
        public static readonly ApplicationError ProductStatusChangeFailed = new(
            ErrorCodes.ProductStatusChangeFailed,
            ErrorMessages.ProductStatusChangeFailed,
            ApplicationErrorType.Internal
        );
    }
}
