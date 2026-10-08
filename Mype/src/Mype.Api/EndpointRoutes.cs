using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Mype.Api.Endpoints.v1.Auth;
using Mype.Application.Auth.Commands.RegisterUser;
using Mype.Shared.Models;
using System.Diagnostics.CodeAnalysis;

namespace Mype.Api
{
    [ExcludeFromCodeCoverage]
    public static class EndpointRoutes
    {
        public static void AddEndpoints(this WebApplication app)
        {
            var apiGroup = app.MapGroup("/api");

            var versionOneGroup = apiGroup.MapGroup("/v1");

            #region Auth

            var authGroup = versionOneGroup
                .MapGroup("/auth")
                .WithTags("Auth");

            authGroup
                .MapPost(
                    "/register",
                    RegisterUserEndpoint.DoAsync
                )
                .AllowAnonymous()
                .WithName("RegisterUser")
                .WithSummary("Registrar usuario")
                .Produces<RegisterUserResult>(
                    StatusCodes.Status201Created
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status400BadRequest
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status409Conflict
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status500InternalServerError
                );

            #endregion
        }
    }
}
