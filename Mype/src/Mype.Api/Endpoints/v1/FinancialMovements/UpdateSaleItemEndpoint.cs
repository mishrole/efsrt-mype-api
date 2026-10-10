using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.FinancialMovements.Commands.UpdateSaleItem;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class UpdateSaleItemEndpoint
    {
        public sealed record UpdateSaleItemRequest(
            Guid ProductId,
            decimal Quantity,
            decimal UnitAmount,
            uint MovementVersion,
            uint ItemVersion
        );

        public static async Task<IResult> DoAsync(
            Guid businessId,
            Guid movementId,
            Guid itemId,
            [FromBody] UpdateSaleItemRequest request,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken token
        )
        {
            var command = new UpdateSaleItemCommand
            {
                BusinessId = businessId,
                MovementId = movementId,
                CurrentUserId = user.GetCurrentUserId(),
                ItemId = itemId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                UnitAmount = request.UnitAmount,
                MovementVersion = request.MovementVersion,
                ItemVersion = request.ItemVersion,
            };
            var result = await sender.Send(command, token);
            return result.ToHttpResult(context, response => TypedResults.Ok(response));
        }
    }
}
