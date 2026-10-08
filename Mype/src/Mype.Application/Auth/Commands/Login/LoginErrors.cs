using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.Auth.Commands.Login
{
    public static class LoginErrors
    {
        public static readonly ApplicationError InvalidCredentials =
            new(
                ErrorCodes.InvalidCredentials,
                ErrorMessages.InvalidCredentials,
                ApplicationErrorType.Unauthorized
            );

        public static readonly ApplicationError AccountUnavailable =
            new(
                ErrorCodes.AccountUnavailable,
                ErrorMessages.AccountUnavailable,
                ApplicationErrorType.Forbidden
            );
    }
}