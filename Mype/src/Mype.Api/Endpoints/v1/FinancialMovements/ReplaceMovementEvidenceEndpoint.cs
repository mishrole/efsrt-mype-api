using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.MovementEvidences.Commands.ReplaceMovementEvidence;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class ReplaceMovementEvidenceEndpoint
    {
        public static async Task<IResult> DoAsync(
            Guid businessId,
            Guid movementId,
            Guid evidenceId,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken token
        )
        {
            var upload = await MovementEvidenceEndpointRequestReader.ReadAsync(
                context.Request,
                true,
                token
            );
            await using var content = upload.File.Content;
            var result = await sender.Send(
                new ReplaceMovementEvidenceCommand
                {
                    BusinessId = businessId,
                    MovementId = movementId,
                    EvidenceId = evidenceId,
                    CurrentUserId = user.GetCurrentUserId(),
                    MovementVersion = upload.MovementVersion,
                    EvidenceVersion = upload.EvidenceVersion,
                    File = upload.File,
                },
                token
            );

            return result.ToHttpResult(context, response => TypedResults.Ok(response));
        }
    }
}
