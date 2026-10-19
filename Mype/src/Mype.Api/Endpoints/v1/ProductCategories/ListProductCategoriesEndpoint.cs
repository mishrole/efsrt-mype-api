using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.ProductCategories.Queries.ListProductCategories;

namespace Mype.Api.Endpoints.v1.ProductCategories
{
    public static class ListProductCategoriesEndpoint
    {
        public static async Task<IResult> DoAsync(
            Guid businessId,
            [FromQuery] string search,
            [FromQuery] bool? isActive,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken token
        )
        {
            var result = await sender.Send(
                new ListProductCategoriesQuery
                {
                    BusinessId = businessId,
                    CurrentUserId = user.GetCurrentUserId(),
                    Search = search,
                    IsActive = isActive,
                },
                token
            );
            return result.ToHttpResult(context, TypedResults.Ok);
        }
    }
}
