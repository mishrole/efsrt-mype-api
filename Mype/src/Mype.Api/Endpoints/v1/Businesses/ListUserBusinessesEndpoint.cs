using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.Businesses.Queries.ListUserBusinesses;

namespace Mype.Api.Endpoints.v1.Businesses
{
    public static class ListUserBusinessesEndpoint
    {
        public static async Task<IResult> DoAsync(
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider userContextProvider,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var query = new ListUserBusinessesQuery
            {
                CurrentUserId = userContextProvider.GetCurrentUserId(),
            };

            var result = await sender.Send(query, cancellationToken);

            return result.ToHttpResult(context, response => TypedResults.Ok(response));
        }
    }
}
