using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.ProductCategories.Commands.CreateProductCategory;

namespace Mype.Api.Endpoints.v1.ProductCategories
{
    public static class CreateProductCategoryEndpoint
    {
        public sealed record Request(string Name, string Color);

        public static async Task<IResult> DoAsync(
            Guid businessId,
            [FromBody] Request request,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider userContextProvider,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var result = await sender.Send(
                new CreateProductCategoryCommand
                {
                    BusinessId = businessId,
                    CurrentUserId = userContextProvider.GetCurrentUserId(),
                    Name = request.Name,
                    Color = request.Color,
                },
                cancellationToken
            );
            return result.ToHttpResult(
                context,
                response =>
                    TypedResults.Created(
                        $"/api/v1/businesses/{businessId}/product-categories/{response.Id}",
                        response
                    )
            );
        }
    }
}
