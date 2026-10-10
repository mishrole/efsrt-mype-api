using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Extensions;
using Mype.Application.Auth.Commands.Login;

namespace Mype.Api.Endpoints.v1.Auth
{
    public static class LoginEndpoint
    {
        public sealed record LoginRequest(string Email, string Password);

        public static async Task<IResult> DoAsync(
            [FromBody] LoginRequest request,
            [FromServices] ISender sender,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var command = new LoginCommand { Email = request.Email, Password = request.Password };

            var result = await sender.Send(command, cancellationToken);

            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Pragma = "no-cache";

            return result.ToHttpResult(context, TypedResults.Ok);
        }
    }
}
