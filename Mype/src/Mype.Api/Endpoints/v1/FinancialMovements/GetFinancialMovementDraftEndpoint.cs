using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.FinancialMovements.Queries.GetFinancialMovementDraft;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class GetFinancialMovementDraftEndpoint
    {
        public static async Task<IResult> DoAsync(Guid businessId, Guid movementId, [FromServices] ISender sender, [FromServices] IUserContextProvider userContextProvider, HttpContext context, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetFinancialMovementDraftQuery { BusinessId = businessId, MovementId = movementId, CurrentUserId = userContextProvider.GetCurrentUserId() }, cancellationToken);
            return result.ToHttpResult(context, response => TypedResults.Ok(response));
        }
    }
}
