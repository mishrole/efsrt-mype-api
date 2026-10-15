using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Common;
using Mype.Api.Extensions;
using Mype.Application.Auth.Commands.RefreshSession;

namespace Mype.Api.Endpoints.v1.Auth
{
    public static class RefreshSessionEndpoint
    {
        public static async Task<IResult> DoAsync(
            [FromServices] ISender sender,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var command = new RefreshSessionCommand
            {
                RefreshToken = AuthCookieManager.ReadRefreshToken(context.Request),
            };
            var result = await sender.Send(command, cancellationToken);

            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Pragma = "no-cache";

            if (result.IsSuccess && result.Value != null)
            {
                AuthCookieManager.WriteRefreshToken(
                    context.Response,
                    result.Value.RefreshToken,
                    result.Value.RefreshTokenExpiresAt
                );
            }
            else
            {
                AuthCookieManager.DeleteRefreshToken(context.Response);
            }

            return result.ToHttpResult(context, TypedResults.Ok);
        }
    }
}
