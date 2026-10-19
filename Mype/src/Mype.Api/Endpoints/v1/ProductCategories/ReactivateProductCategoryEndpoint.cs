using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.ProductCategories.Commands.ReactivateProductCategory;

namespace Mype.Api.Endpoints.v1.ProductCategories
{
    public static class ReactivateProductCategoryEndpoint
    {
        public sealed record Request(uint Version);

        public static async Task<IResult> DoAsync(
            Guid businessId,
            Guid productCategoryId,
            [FromBody] Request request,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken token
        )
        {
            var result = await sender.Send(
                new ReactivateProductCategoryCommand
                {
                    BusinessId = businessId,
                    ProductCategoryId = productCategoryId,
                    CurrentUserId = user.GetCurrentUserId(),
                    Version = request.Version,
                },
                token
            );
            return result.ToHttpResult(context, TypedResults.Ok);
        }
    }
}
