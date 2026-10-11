using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.MovementEvidences.Commands.RemoveMovementEvidence;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class RemoveMovementEvidenceEndpoint
    {
        public sealed record Request(uint MovementVersion, uint EvidenceVersion);

        public static async Task<IResult> DoAsync(
            Guid businessId,
            Guid movementId,
            Guid evidenceId,
            [FromBody] Request request,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken token
        )
        {
            var result = await sender.Send(
                new RemoveMovementEvidenceCommand
                {
                    BusinessId = businessId,
                    MovementId = movementId,
                    EvidenceId = evidenceId,
                    CurrentUserId = user.GetCurrentUserId(),
                    MovementVersion = request.MovementVersion,
                    EvidenceVersion = request.EvidenceVersion,
                },
                token
            );

            return result.ToHttpResult(context, _ => TypedResults.NoContent());
        }
    }
}
