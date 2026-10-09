using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.Businesses.Commands.CreateBusiness
{
    public static class CreateBusinessErrors
    {
        public static readonly ApplicationError
            UnsupportedCurrency =
                new(
                    ErrorCodes.UnsupportedCurrency,
                    ErrorMessages.UnsupportedCurrency,
                    ApplicationErrorType.UnprocessableEntity
                );

        public static readonly ApplicationError
            SystemRoleUnavailable =
                new(
                    ErrorCodes.SystemRoleUnavailable,
                    ErrorMessages.SystemRoleUnavailable,
                    ApplicationErrorType.Internal
                );

        public static readonly ApplicationError
            BusinessCreationFailed =
                new(
                    ErrorCodes.BusinessCreationFailed,
                    ErrorMessages.BusinessCreationFailed,
                    ApplicationErrorType.Internal
                );

        public static readonly ApplicationError
            RucAlreadyRegistered =
                new(
                    ErrorCodes.RucAlreadyRegistered,
                    ErrorMessages.RucAlreadyRegistered,
                    ApplicationErrorType.Conflict
                );
    }
}