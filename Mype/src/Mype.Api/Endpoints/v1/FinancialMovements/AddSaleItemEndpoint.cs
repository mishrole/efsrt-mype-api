using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.FinancialMovements.Commands.AddSaleItem;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class AddSaleItemEndpoint
    {
        public sealed record AddSaleItemRequest(
            Guid ProductId,
            decimal Quantity,
            decimal UnitAmount,
            uint MovementVersion
        );

        public static async Task<IResult> DoAsync(
            Guid businessId,
            Guid movementId,
            [FromBody] AddSaleItemRequest request,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken token
        )
        {
            var command = new AddSaleItemCommand
            {
                BusinessId = businessId,
                MovementId = movementId,
                CurrentUserId = user.GetCurrentUserId(),
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                UnitAmount = request.UnitAmount,
                MovementVersion = request.MovementVersion,
            };
            var result = await sender.Send(command, token);
            return result.ToHttpResult(
                context,
                response =>
                    TypedResults.Created(
                        $"/api/v1/businesses/{businessId}/financial-movements/{movementId}/sale-items/{response.Item.Id}",
                        response
                    )
            );
        }
    }
}
