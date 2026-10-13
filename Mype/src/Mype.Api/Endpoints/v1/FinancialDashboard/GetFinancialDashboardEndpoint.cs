using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.FinancialDashboard.Enums;
using Mype.Application.FinancialDashboard.Queries.GetFinancialDashboard;

namespace Mype.Api.Endpoints.v1.FinancialDashboard
{
    public static class GetFinancialDashboardEndpoint
    {
        public static async Task<IResult> DoAsync(
            Guid businessId,
            DateOnly? dateFrom,
            DateOnly? dateTo,
            FinancialDashboardGranularity granularity,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider user,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var result = await sender.Send(
                new GetFinancialDashboardQuery
                {
                    BusinessId = businessId,
                    CurrentUserId = user.GetCurrentUserId(),
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    Granularity = granularity,
                },
                cancellationToken
            );
            return result.ToHttpResult(context, TypedResults.Ok);
        }
    }
}
