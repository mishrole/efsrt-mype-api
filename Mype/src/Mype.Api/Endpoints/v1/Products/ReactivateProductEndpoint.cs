using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.Products.Commands.ReactivateProduct;

namespace Mype.Api.Endpoints.v1.Products
{
    public static class ReactivateProductEndpoint
    {
        public sealed record ReactivateProductRequest(uint Version);

        public static async Task<IResult> DoAsync(
            Guid businessId,
            Guid productId,
            [FromBody] ReactivateProductRequest request,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider userContextProvider,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var command = new ReactivateProductCommand
            {
                BusinessId = businessId,
                ProductId = productId,
                CurrentUserId = userContextProvider.GetCurrentUserId(),
                Version = request.Version,
            };
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(context, response => TypedResults.Ok(response));
        }
    }
}
