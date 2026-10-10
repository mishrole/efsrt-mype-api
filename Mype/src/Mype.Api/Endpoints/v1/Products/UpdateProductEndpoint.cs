using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.Products.Commands.UpdateProduct;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace Mype.Api.Endpoints.v1.Products
{
    public static class UpdateProductEndpoint
    {
        public sealed record UpdateProductRequest(string Name, Guid CategoryId, decimal SalePrice, decimal UnitCost, uint Version);
        public static async Task<IResult> DoAsync(Guid businessId, Guid productId, [FromBody] UpdateProductRequest request, [FromServices] ISender sender, [FromServices] IUserContextProvider userContextProvider, HttpContext context, CancellationToken cancellationToken)
        {
            var command = new UpdateProductCommand { BusinessId = businessId, ProductId = productId, CurrentUserId = userContextProvider.GetCurrentUserId(), Name = request.Name, CategoryId = request.CategoryId, SalePrice = request.SalePrice, UnitCost = request.UnitCost, Version = request.Version };
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(context, response => TypedResults.Ok(response));
        }
    }
}
