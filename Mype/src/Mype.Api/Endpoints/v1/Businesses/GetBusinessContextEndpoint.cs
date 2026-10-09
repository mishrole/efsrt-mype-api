using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.Businesses.Queries.GetBusinessContext;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Api.Endpoints.v1.Businesses
{
    public static class GetBusinessContextEndpoint
    {
        public static async Task<IResult> DoAsync(
            Guid businessId,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider userContextProvider,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var query = new GetBusinessContextQuery
            {
                BusinessId = businessId,
                CurrentUserId =
                    userContextProvider.GetCurrentUserId()
            };

            var result = await sender.Send(
                query,
                cancellationToken
            );

            return result.ToHttpResult(
                context,
                response => TypedResults.Ok(response)
            );
        }
    }
}