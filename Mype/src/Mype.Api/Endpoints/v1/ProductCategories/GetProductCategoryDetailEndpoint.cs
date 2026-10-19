using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.ProductCategories.Queries.GetProductCategoryDetail;

namespace Mype.Api.Endpoints.v1.ProductCategories
{
    public static class GetProductCategoryDetailEndpoint
    {
        public static async Task<IResult> DoAsync(
            Guid businessId,
            Guid productCategoryId,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken token
        )
        {
            var result = await sender.Send(
                new GetProductCategoryDetailQuery
                {
                    BusinessId = businessId,
                    ProductCategoryId = productCategoryId,
                    CurrentUserId = user.GetCurrentUserId(),
                },
                token
            );
            return result.ToHttpResult(context, TypedResults.Ok);
        }
    }
}
