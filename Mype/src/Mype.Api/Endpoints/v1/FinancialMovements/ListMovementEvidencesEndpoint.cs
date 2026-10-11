using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.MovementEvidences.Queries.ListMovementEvidences;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class ListMovementEvidencesEndpoint
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
            var result = await sender.Send(
                new ListMovementEvidencesQuery
                {
                    BusinessId = businessId,
                    MovementId = movementId,
                    CurrentUserId = user.GetCurrentUserId(),
                },
                token
            );
            return result.ToHttpResult(context, response => TypedResults.Ok(response));
        }
    }
}
