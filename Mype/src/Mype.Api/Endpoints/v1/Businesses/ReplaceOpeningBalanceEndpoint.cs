using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.Businesses.Commands.ReplaceOpeningBalance;

namespace Mype.Api.Endpoints.v1.Businesses
{
    public static class ReplaceOpeningBalanceEndpoint
    {
        public sealed record Request(decimal Amount, string Reason, uint MovementVersion);

        public static async Task<IResult> DoAsync(
            Guid businessId,
            [FromBody] Request request,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var result = await sender.Send(
                new ReplaceOpeningBalanceCommand
                {
                    BusinessId = businessId,
                    CurrentUserId = user.GetCurrentUserId(),
                    Amount = request.Amount,
                    Reason = request.Reason,
                    MovementVersion = request.MovementVersion,
                },
                cancellationToken
            );
            return result.ToHttpResult(context, TypedResults.Ok);
        }
    }
}
