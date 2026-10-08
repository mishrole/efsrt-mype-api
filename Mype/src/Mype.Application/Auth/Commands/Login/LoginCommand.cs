using MediatR;
using Mype.Application.Common;

namespace Mype.Application.Auth.Commands.Login
{
    public class LoginCommand : IRequest<Result<LoginResult>>
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}