using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.FinancialMovements.Queries.GetFinancialMovementHistoryDetail;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class GetFinancialMovementHistoryDetailEndpoint
    {
        public static async Task<IResult> DoAsync(
            Guid businessId,
            Guid movementId,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var result = await sender.Send(
                new GetFinancialMovementHistoryDetailQuery
                {
                    BusinessId = businessId,
                    MovementId = movementId,
                    CurrentUserId = user.GetCurrentUserId(),
                },
                cancellationToken
            );
            return result.ToHttpResult(context, TypedResults.Ok);
        }
    }
}
