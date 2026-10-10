using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.FinancialMovements.Commands.UpdateExpenseItem;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class UpdateExpenseItemEndpoint
    {
        public sealed record UpdateExpenseItemRequest(
            Guid CategoryId,
            string Description,
            decimal Quantity,
            decimal UnitAmount,
            uint MovementVersion,
            uint ItemVersion
        );

        public static async Task<IResult> DoAsync(
            Guid businessId,
            Guid movementId,
            Guid itemId,
            [FromBody] UpdateExpenseItemRequest request,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken token
        )
        {
            var command = new UpdateExpenseItemCommand
            {
                BusinessId = businessId,
                MovementId = movementId,
                ItemId = itemId,
                CurrentUserId = user.GetCurrentUserId(),
                CategoryId = request.CategoryId,
                Description = request.Description,
                Quantity = request.Quantity,
                UnitAmount = request.UnitAmount,
                MovementVersion = request.MovementVersion,
                ItemVersion = request.ItemVersion,
            };
            var result = await sender.Send(command, token);
            return result.ToHttpResult(context, TypedResults.Ok);
        }
    }
}
