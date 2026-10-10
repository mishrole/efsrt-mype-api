using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.Auth.Commands.RegisterUser
{
    public static class RegisterUserErrors
    {
        public static readonly ApplicationError EmailAlreadyRegistered = new(
            ErrorCodes.EmailAlreadyRegistered,
            ErrorMessages.EmailAlreadyRegistered,
            ApplicationErrorType.Conflict
        );
    }
}
