using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.Products.Queries.ListProducts
{
    public static class ListProductsErrors
    {
        public static readonly ApplicationError
            BusinessAccessForbidden =
                new(
                    ErrorCodes
                        .BusinessAccessForbidden,
                    ErrorMessages
                        .BusinessAccessForbidden,
                    ApplicationErrorType.Forbidden
                );

        public static readonly ApplicationError
            BusinessUnavailable =
                new(
                    ErrorCodes.BusinessUnavailable,
                    ErrorMessages
                        .BusinessUnavailable,
                    ApplicationErrorType.Conflict
                );

        public static readonly ApplicationError
            ProductAccessForbidden =
                new(
                    ErrorCodes
                        .ProductAccessForbidden,
                    ErrorMessages
                        .ProductAccessForbidden,
                    ApplicationErrorType.Forbidden
                );

        public static readonly ApplicationError
            CategoryNotFound =
                new(
                    ErrorCodes.CategoryNotFound,
                    ErrorMessages.CategoryNotFound,
                    ApplicationErrorType.NotFound
                );
    }
}