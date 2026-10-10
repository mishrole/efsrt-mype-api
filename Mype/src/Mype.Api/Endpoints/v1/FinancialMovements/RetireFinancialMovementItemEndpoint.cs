using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.FinancialMovements.Commands.RetireFinancialMovementItem;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class RetireFinancialMovementItemEndpoint
    {
        public sealed record RetireFinancialMovementItemRequest(
            uint MovementVersion,
            uint ItemVersion
        );

        public static async Task<IResult> DoAsync(
            Guid businessId,
            Guid movementId,
            Guid itemId,
            [FromBody] RetireFinancialMovementItemRequest request,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken token
        )
        {
            var command = new RetireFinancialMovementItemCommand
            {
                BusinessId = businessId,
                MovementId = movementId,
                CurrentUserId = user.GetCurrentUserId(),
                ItemId = itemId,
                MovementVersion = request.MovementVersion,
                ItemVersion = request.ItemVersion,
            };
            var result = await sender.Send(command, token);
            return result.ToHttpResult(context, response => TypedResults.Ok(response));
        }
    }
}
