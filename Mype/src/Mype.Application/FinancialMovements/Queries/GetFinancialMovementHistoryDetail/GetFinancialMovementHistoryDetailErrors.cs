using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Queries.GetFinancialMovementHistoryDetail
{
    public static class GetFinancialMovementHistoryDetailErrors
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
        public static readonly ApplicationError MovementNotFound = new(
            ErrorCodes.MovementNotFound,
            ErrorMessages.MovementNotFound,
            ApplicationErrorType.NotFound
        );
        public static readonly ApplicationError QueryFailed = new(
            ErrorCodes.MovementDetailQueryFailed,
            ErrorMessages.MovementDetailQueryFailed,
            ApplicationErrorType.Internal
        );
    }
}
