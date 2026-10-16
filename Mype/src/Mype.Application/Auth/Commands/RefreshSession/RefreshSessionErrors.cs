using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.Auth.Commands.RefreshSession
{
    public static class RefreshSessionErrors
    {
        public static readonly ApplicationError SessionUnavailable = new(
            ErrorCodes.SessionUnavailable,
            ErrorMessages.SessionUnavailable,
            ApplicationErrorType.Unauthorized
        );

        public static readonly ApplicationError SessionReuseDetected = new(
            ErrorCodes.SessionReuseDetected,
            ErrorMessages.SessionReuseDetected,
            ApplicationErrorType.Unauthorized
        );

        public static readonly ApplicationError SessionRefreshFailed = new(
            ErrorCodes.SessionRefreshFailed,
            ErrorMessages.SessionRefreshFailed,
            ApplicationErrorType.Internal
        );
    }
}
