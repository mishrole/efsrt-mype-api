using MediatR;
using Mype.Application.Common;

namespace Mype.Application.Auth.Commands.RegisterUser
{
    public class RegisterUserCommand : IRequest<Result<RegisterUserResult>>
    {
        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string PasswordConfirmation { get; set; } = string.Empty;
    }
}
