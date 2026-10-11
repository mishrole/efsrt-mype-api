using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.FinancialMovements.Commands.DiscardFinancialMovement;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class DiscardFinancialMovementEndpoint
    {
        public sealed record Request(
            uint MovementVersion,
            IReadOnlyCollection<MovementEvidenceVersion> EvidenceVersions
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
                new DiscardFinancialMovementCommand
                {
                    BusinessId = businessId,
                    MovementId = movementId,
                    CurrentUserId = user.GetCurrentUserId(),
                    MovementVersion = request.MovementVersion,
                    EvidenceVersions = request.EvidenceVersions,
                },
                cancellationToken
            );
            return result.ToHttpResult(context, TypedResults.Ok);
        }
    }
}
