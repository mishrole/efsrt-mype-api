using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.Products.Queries.ListProducts;

namespace Mype.Api.Endpoints.v1.Products
{
    public static class ListProductsEndpoint
    {
        public static async Task<IResult> DoAsync(
            Guid businessId,
            [FromQuery] string search,
            [FromQuery] Guid? categoryId,
            [FromQuery] bool? isActive,
            [FromQuery] bool? availableForSale,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider userContextProvider,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var query = new ListProductsQuery
            {
                BusinessId = businessId,
                CurrentUserId = userContextProvider.GetCurrentUserId(),
                Search = search,
                CategoryId = categoryId,
                IsActive = isActive,
                AvailableForSale = availableForSale ?? false,
            };

            var result = await sender.Send(query, cancellationToken);

            return result.ToHttpResult(context, response => TypedResults.Ok(response));
        }
    }
}
