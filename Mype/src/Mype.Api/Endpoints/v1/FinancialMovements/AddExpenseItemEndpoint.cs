using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.FinancialMovements.Commands.AddExpenseItem;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class AddExpenseItemEndpoint
    {
        public sealed record AddExpenseItemRequest(
            Guid CategoryId,
            string Description,
            decimal Quantity,
            decimal UnitAmount,
            uint MovementVersion
        );

        public static async Task<IResult> DoAsync(
            Guid businessId,
            Guid movementId,
            [FromBody] AddExpenseItemRequest request,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken token
        )
        {
            var command = new AddExpenseItemCommand
            {
                BusinessId = businessId,
                MovementId = movementId,
                CurrentUserId = user.GetCurrentUserId(),
                CategoryId = request.CategoryId,
                Description = request.Description,
                Quantity = request.Quantity,
                UnitAmount = request.UnitAmount,
                MovementVersion = request.MovementVersion,
            };
            var result = await sender.Send(command, token);
            return result.ToHttpResult(
                context,
                response =>
                    TypedResults.Created(
                        $"/api/v1/businesses/{businessId}/financial-movements/{movementId}/expense-items/{response.Item.Id}",
                        response
                    )
            );
        }
    }
}
