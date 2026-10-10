using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.FinancialMovements.Commands.CreateFinancialMovement;
using Mype.Domain.FinancialMovements;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class CreateFinancialMovementEndpoint
    {
        public sealed record CreateFinancialMovementRequest(
            FinancialMovementType Type,
            DateOnly MovementDate,
            string Description
        );

        public static async Task<IResult> DoAsync(
            Guid businessId,
            [FromBody] CreateFinancialMovementRequest request,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider userContextProvider,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var command = new CreateFinancialMovementCommand
            {
                BusinessId = businessId,
                CurrentUserId = userContextProvider.GetCurrentUserId(),
                Type = request.Type,
                MovementDate = request.MovementDate,
                Description = request.Description,
            };
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(
                context,
                response =>
                    TypedResults.Created(
                        $"/api/v1/businesses/{businessId}/financial-movements/{response.Id}",
                        response
                    )
            );
        }
    }
}
