using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.Products.Commands.DeactivateProduct;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace Mype.Api.Endpoints.v1.Products
{
    public static class DeactivateProductEndpoint
    {
        public sealed record DeactivateProductRequest(uint Version);
        public static async Task<IResult> DoAsync(Guid businessId, Guid productId, [FromBody] DeactivateProductRequest request, [FromServices] ISender sender, [FromServices] IUserContextProvider userContextProvider, HttpContext context, CancellationToken cancellationToken)
        {
            var command = new DeactivateProductCommand { BusinessId = businessId, ProductId = productId, CurrentUserId = userContextProvider.GetCurrentUserId(), Version = request.Version };
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(context, response => TypedResults.Ok(response));
        }
    }
}
