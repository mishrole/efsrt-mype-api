using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.Products.Queries.GetProductDetail;

namespace Mype.Api.Endpoints.v1.Products
{
    public static class GetProductDetailEndpoint
    {
        public static async Task<IResult> DoAsync(
            Guid businessId,
            Guid productId,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider userContextProvider,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var query = new GetProductDetailQuery
            {
                BusinessId = businessId,
                ProductId = productId,
                CurrentUserId = userContextProvider.GetCurrentUserId(),
            };

            var result = await sender.Send(query, cancellationToken);

            return result.ToHttpResult(context, response => TypedResults.Ok(response));
        }
    }
}
