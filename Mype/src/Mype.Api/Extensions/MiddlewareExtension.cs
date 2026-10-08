using Microsoft.AspNetCore.Builder;
using Mype.Api.Middlewares;
using System.Diagnostics.CodeAnalysis;

namespace Mype.Api.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class MiddlewareExtension
    {
        public static IApplicationBuilder AddMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseMiddleware<AuthenticationHandlingMiddleware>();

            return app;
        }
    }
}
