using MediatR;
using Mype.Application.Common;

namespace Mype.Application.Auth.Commands.RefreshSession
{
    public sealed class RefreshSessionCommand : IRequest<Result<RefreshSessionResult>>
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
