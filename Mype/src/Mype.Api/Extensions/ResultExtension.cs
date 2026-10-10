using System;
using Microsoft.AspNetCore.Http;
using Mype.Api.Common;
using Mype.Application.Common;
using Mype.Shared.Constants;

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
                return success(
                    result.Value
                        ?? throw new InvalidOperationException(
                            ErrorMessages.SuccessfulResultWithoutValue
                        )
                );
            }

            var error =
                result.Error
                ?? throw new InvalidOperationException(ErrorMessages.FailedResultWithoutError);

            var response = HttpErrorMapper.FromApplicationError(error, context.TraceIdentifier);

            return Results.Json(response, statusCode: response.StatusCode);
        }
    }
}
