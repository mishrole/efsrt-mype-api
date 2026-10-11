using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.FinancialMovements.Commands.CancelFinancialMovement;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class CancelFinancialMovementEndpoint
    {
        public sealed record Request(string Reason, uint MovementVersion);

        public static async Task<IResult> DoAsync(
            Guid businessId,
            Guid movementId,
            [FromBody] Request request,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var result = await sender.Send(
                new CancelFinancialMovementCommand
                {
                    BusinessId = businessId,
                    MovementId = movementId,
                    CurrentUserId = user.GetCurrentUserId(),
                    Reason = request.Reason,
                    MovementVersion = request.MovementVersion,
                },
                cancellationToken
            );
            return result.ToHttpResult(context, TypedResults.Ok);
        }
    }
}
