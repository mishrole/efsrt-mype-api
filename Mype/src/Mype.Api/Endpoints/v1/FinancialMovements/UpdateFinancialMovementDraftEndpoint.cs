using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.FinancialMovements.Commands.UpdateFinancialMovementDraft;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class UpdateFinancialMovementDraftEndpoint
    {
        public sealed record UpdateFinancialMovementDraftRequest(
            DateOnly MovementDate,
            string Description,
            uint Version
        );

        public static async Task<IResult> DoAsync(
            Guid businessId,
            Guid movementId,
            [FromBody] UpdateFinancialMovementDraftRequest request,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider userContextProvider,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var command = new UpdateFinancialMovementDraftCommand
            {
                BusinessId = businessId,
                MovementId = movementId,
                CurrentUserId = userContextProvider.GetCurrentUserId(),
                MovementDate = request.MovementDate,
                Description = request.Description,
                Version = request.Version,
            };
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(context, response => TypedResults.Ok(response));
        }
    }
}
