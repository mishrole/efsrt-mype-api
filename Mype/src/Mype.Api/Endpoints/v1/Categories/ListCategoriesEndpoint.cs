using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.Categories.Queries.ListCategories;
using Mype.Domain.Categories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Api.Endpoints.v1.Categories
{
    public static class ListCategoriesEndpoint
    {
        public static async Task<IResult> DoAsync(
            Guid businessId,
            [FromQuery] CategoryType? type,
            [FromQuery] bool? isActive,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider userContextProvider,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var query = new ListCategoriesQuery
            {
                BusinessId = businessId,
                CurrentUserId =
                    userContextProvider
                        .GetCurrentUserId(),
                Type = type,
                IsActive = isActive
            };

            var result = await sender.Send(
                query,
                cancellationToken
            );

            return result.ToHttpResult(
                context,
                response =>
                    TypedResults.Ok(response)
            );
        }
    }
}
