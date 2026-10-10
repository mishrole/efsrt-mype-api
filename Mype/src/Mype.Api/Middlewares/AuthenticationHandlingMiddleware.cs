using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Mype.Api.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Api.Middlewares
{
    public class AuthenticationHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            if (
                context.Response.StatusCode != StatusCodes.Status401Unauthorized
                || context.Response.HasStarted
                || context.Response.ContentLength.HasValue
            )
            {
                return;
            }

            var response = HttpErrorMapper.FromApplicationError(
                ErrorCodes.Unauthenticated,
                ErrorMessages.Unauthenticated,
                ApplicationErrorType.Unauthorized,
                context.TraceIdentifier
            );

            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
        }
    }
}
