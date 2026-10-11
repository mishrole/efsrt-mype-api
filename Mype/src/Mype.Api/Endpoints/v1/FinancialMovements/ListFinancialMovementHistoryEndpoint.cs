using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.FinancialMovements.Queries.ListFinancialMovementHistory;
using Mype.Domain.FinancialMovements;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    public static class ListFinancialMovementHistoryEndpoint
    {
        public static async Task<IResult> DoAsync(
            Guid businessId,
            FinancialMovementType? type,
            FinancialMovementStatus? status,
            DateOnly? dateFrom,
            DateOnly? dateTo,
            Guid? categoryId,
            int page,
            int pageSize,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var result = await sender.Send(
                new ListFinancialMovementHistoryQuery
                {
                    BusinessId = businessId,
                    CurrentUserId = user.GetCurrentUserId(),
                    Type = type,
                    Status = status,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    CategoryId = categoryId,
                    Page = page == 0 ? 1 : page,
                    PageSize = pageSize == 0 ? 20 : pageSize,
                },
                cancellationToken
            );
            return result.ToHttpResult(context, TypedResults.Ok);
        }
    }
}
