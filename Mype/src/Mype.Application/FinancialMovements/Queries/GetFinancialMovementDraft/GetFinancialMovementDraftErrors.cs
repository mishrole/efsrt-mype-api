using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Queries.GetFinancialMovementDraft
{
    public static class GetFinancialMovementDraftErrors
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
    }
}
