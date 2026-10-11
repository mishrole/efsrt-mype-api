using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Queries.ListFinancialMovementHistory
{
    public static class ListFinancialMovementHistoryErrors
    {
        public static readonly ApplicationError BusinessAccessForbidden = new(
            ErrorCodes.BusinessAccessForbidden,
            ErrorMessages.BusinessAccessForbidden,
            ApplicationErrorType.Forbidden
        );
        public static readonly ApplicationError MovementAccessForbidden = new(
            ErrorCodes.MovementAccessForbidden,
            ErrorMessages.MovementAccessForbidden,
            ApplicationErrorType.Forbidden
        );
        public static readonly ApplicationError CategoryNotFound = new(
            ErrorCodes.CategoryNotFound,
            ErrorMessages.CategoryNotFound,
            ApplicationErrorType.NotFound
        );
        public static readonly ApplicationError QueryFailed = new(
            ErrorCodes.FinancialHistoryQueryFailed,
            ErrorMessages.FinancialHistoryQueryFailed,
            ApplicationErrorType.Internal
        );
    }
}
