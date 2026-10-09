using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.Products.Commands.CreateProduct;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Api.Endpoints.v1.Products
{
    public static class CreateProductEndpoint
    {
        public sealed record CreateProductRequest(
            string Name,
            Guid CategoryId,
            decimal SalePrice,
            decimal UnitCost
        );

        public static async Task<IResult> DoAsync(
            Guid businessId,
            [FromBody]
                CreateProductRequest request,
            [FromServices] ISender sender,
            [FromServices]
                IUserContextProvider
                userContextProvider,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var command =
                new CreateProductCommand
                {
                    BusinessId = businessId,
                    CurrentUserId =
                        userContextProvider
                            .GetCurrentUserId(),
                    CategoryId =
                        request.CategoryId,
                    Name = request.Name,
                    SalePrice =
                        request.SalePrice,
                    UnitCost =
                        request.UnitCost
                };

            var result = await sender.Send(
                command,
                cancellationToken
            );

            return result.ToHttpResult(
                context,
                response =>
                    TypedResults.Created(
                        $"/api/v1/businesses/{businessId}/products/{response.Id}",
                        response
                    )
            );
        }
    }
}