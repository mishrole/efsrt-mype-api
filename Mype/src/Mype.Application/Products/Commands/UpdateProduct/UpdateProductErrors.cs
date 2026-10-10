using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.Products.Commands.UpdateProduct
{
    public static class UpdateProductErrors
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
            ErrorCodes.ProductConcurrencyConflict,
            ErrorMessages.ProductConcurrencyConflict,
            ApplicationErrorType.Conflict
        );

        public static readonly ApplicationError CategoryNotFound = new(
            ErrorCodes.CategoryNotFound,
            ErrorMessages.CategoryNotFound,
            ApplicationErrorType.NotFound
        );
        public static readonly ApplicationError CategoryUnavailable = new(
            ErrorCodes.CategoryUnavailable,
            ErrorMessages.CategoryUnavailable,
            ApplicationErrorType.UnprocessableEntity
        );
        public static readonly ApplicationError ProductCategoryMustBeSale = new(
            ErrorCodes.ProductCategoryMustBeSale,
            ErrorMessages.ProductCategoryMustBeSale,
            ApplicationErrorType.UnprocessableEntity
        );
        public static readonly ApplicationError ProductAlreadyExists = new(
            ErrorCodes.ProductAlreadyExists,
            ErrorMessages.ProductAlreadyExists,
            ApplicationErrorType.Conflict
        );
        public static readonly ApplicationError ProductUpdateFailed = new(
            ErrorCodes.ProductUpdateFailed,
            ErrorMessages.ProductUpdateFailed,
            ApplicationErrorType.Internal
        );
    }
}
