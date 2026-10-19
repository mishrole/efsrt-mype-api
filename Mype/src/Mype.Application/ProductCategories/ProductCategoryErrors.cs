using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.ProductCategories
{
    public static class ProductCategoryErrors
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
        public static readonly ApplicationError AccessForbidden = new(
            "PRODUCT_CATEGORY_ACCESS_FORBIDDEN",
            "No tiene permiso para gestionar categorías de producto.",
            ApplicationErrorType.Forbidden
        );
        public static readonly ApplicationError NotFound = new(
            ErrorCodes.ProductCategoryNotFound,
            ErrorMessages.ProductCategoryNotFound,
            ApplicationErrorType.NotFound
        );
        public static readonly ApplicationError AlreadyExists = new(
            ErrorCodes.ProductCategoryAlreadyExists,
            ErrorMessages.ProductCategoryAlreadyExists,
            ApplicationErrorType.Conflict
        );
        public static readonly ApplicationError ReactivationRequired = new(
            "PRODUCT_CATEGORY_REACTIVATION_REQUIRED",
            "La categoría de producto ya existe inactiva y debe reactivarse.",
            ApplicationErrorType.Conflict
        );
        public static readonly ApplicationError AlreadyActive = new(
            "PRODUCT_CATEGORY_ALREADY_ACTIVE",
            "La categoría de producto ya está activa.",
            ApplicationErrorType.Conflict
        );
        public static readonly ApplicationError AlreadyInactive = new(
            "PRODUCT_CATEGORY_ALREADY_INACTIVE",
            "La categoría de producto ya está inactiva.",
            ApplicationErrorType.Conflict
        );
        public static readonly ApplicationError ConcurrencyConflict = new(
            ErrorCodes.ConcurrencyConflict,
            ErrorMessages.ConcurrencyConflict,
            ApplicationErrorType.Conflict
        );
        public static readonly ApplicationError PersistenceFailed = new(
            "PRODUCT_CATEGORY_PERSISTENCE_FAILED",
            "No fue posible guardar la categoría de producto.",
            ApplicationErrorType.Internal
        );
    }
}
