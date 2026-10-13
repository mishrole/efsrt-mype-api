using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialDashboard.Queries.GetFinancialDashboard
{
    public static class GetFinancialDashboardErrors
    {
        public static readonly ApplicationError BusinessAccessForbidden = new(
            ErrorCodes.BusinessAccessForbidden,
            ErrorMessages.BusinessAccessForbidden,
            ApplicationErrorType.Forbidden
        );
        public static readonly ApplicationError DashboardAccessForbidden = new(
            ErrorCodes.DashboardAccessForbidden,
            ErrorMessages.DashboardAccessForbidden,
            ApplicationErrorType.Forbidden
        );
        public static readonly ApplicationError OpeningBalanceNotFound = new(
            ErrorCodes.OpeningBalanceNotFound,
            ErrorMessages.OpeningBalanceNotFound,
            ApplicationErrorType.Conflict
        );
        public static readonly ApplicationError QueryFailed = new(
            ErrorCodes.FinancialDashboardQueryFailed,
            ErrorMessages.FinancialDashboardQueryFailed,
            ApplicationErrorType.Internal
        );
    }
}
