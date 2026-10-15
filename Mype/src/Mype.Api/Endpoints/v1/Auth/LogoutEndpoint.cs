using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Common;
using Mype.Application.Auth.Commands.Logout;

namespace Mype.Api.Endpoints.v1.Auth
{
    public static class LogoutEndpoint
    {
        public static async Task<IResult> DoAsync(
            [FromServices] ISender sender,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var command = new LogoutCommand
            {
                RefreshToken = AuthCookieManager.ReadRefreshToken(context.Request),
            };

            await sender.Send(command, cancellationToken);
            AuthCookieManager.DeleteRefreshToken(context.Response);
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Pragma = "no-cache";
            return TypedResults.NoContent();
        }
    }
}
