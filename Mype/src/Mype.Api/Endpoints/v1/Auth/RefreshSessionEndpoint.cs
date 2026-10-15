using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Common;
using Mype.Api.Extensions;
using Mype.Application.Auth.Commands.RefreshSession;
using Mype.Application.Common;

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
            var refreshToken = AuthCookieManager.ReadRefreshToken(context.Request);

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                var unavailableResult = Result<RefreshSessionResult>.Failure(
                    RefreshSessionErrors.SessionUnavailable
                );

                AuthCookieManager.DeleteRefreshToken(context.Response);

                return unavailableResult.ToHttpResult(context, TypedResults.Ok);
            }

            var result = await sender.Send(
                new RefreshSessionCommand { RefreshToken = refreshToken },
                cancellationToken
            );

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

            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Pragma = "no-cache";

            return result.ToHttpResult(context, TypedResults.Ok);
        }
    }
}
