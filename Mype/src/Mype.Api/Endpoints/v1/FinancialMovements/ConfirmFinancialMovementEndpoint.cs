using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.FinancialMovements.Commands.ConfirmFinancialMovement;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class ConfirmFinancialMovementEndpoint
    {
        public sealed record Request(
            uint MovementVersion,
            IReadOnlyCollection<FinancialMovementItemVersion> ItemVersions
        );

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
                new ConfirmFinancialMovementCommand
                {
                    BusinessId = businessId,
                    MovementId = movementId,
                    CurrentUserId = user.GetCurrentUserId(),
                    MovementVersion = request.MovementVersion,
                    ItemVersions = request.ItemVersions,
                },
                cancellationToken
            );
            return result.ToHttpResult(context, TypedResults.Ok);
        }
    }
}
