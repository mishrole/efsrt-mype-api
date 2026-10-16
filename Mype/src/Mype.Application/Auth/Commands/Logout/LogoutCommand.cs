using MediatR;
using Mype.Application.Common;

namespace Mype.Application.Auth.Commands.Logout
{
    public sealed class LogoutCommand : IRequest<Result<LogoutResult>>
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
