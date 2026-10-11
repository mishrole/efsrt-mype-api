using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.MovementEvidences.Commands.AttachMovementEvidence;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class AttachMovementEvidenceEndpoint
    {
        public static async Task<IResult> DoAsync(
            Guid businessId,
            Guid movementId,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken token
        )
        {
            var upload = await MovementEvidenceEndpointRequestReader.ReadAsync(
                context.Request,
                false,
                token
            );
            await using var content = upload.File.Content;
            var result = await sender.Send(
                new AttachMovementEvidenceCommand
                {
                    BusinessId = businessId,
                    MovementId = movementId,
                    CurrentUserId = user.GetCurrentUserId(),
                    MovementVersion = upload.MovementVersion,
                    File = upload.File,
                },
                token
            );

            return result.ToHttpResult(
                context,
                response =>
                    TypedResults.Created(
                        $"/api/v1/businesses/{businessId}/financial-movements/{movementId}/evidences/{response.Id}",
                        response
                    )
            );
        }
    }
}
