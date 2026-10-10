using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.Categories.Queries.ListCategories
{
    public static class ListCategoriesErrors
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

        public static readonly ApplicationError CategoryAccessForbidden = new(
            ErrorCodes.CategoryAccessForbidden,
            ErrorMessages.CategoryAccessForbidden,
            ApplicationErrorType.Forbidden
        );
    }
}
