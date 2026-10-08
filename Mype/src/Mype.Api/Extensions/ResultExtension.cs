using Microsoft.AspNetCore.Http;
using Mype.Api.Common;
using Mype.Application.Common;
using System;

namespace Mype.Api.Extensions
{
    public static class ResultExtension
    {
        public static IResult ToHttpResult<T>(
            this Result<T> result,
            HttpContext context,
            Func<T, IResult> success
        )
        {
            if (result.IsSuccess)
            {
                return success(result.Value);
            }

            var response = HttpErrorMapper.FromApplicationError(
                result.Error,
                context.TraceIdentifier
            );

            return Results.Json(
                response,
                statusCode: response.StatusCode
            );
        }
    }
}