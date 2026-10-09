using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Mype.Api.Endpoints.v1.Auth;
using Mype.Api.Endpoints.v1.Businesses;
using Mype.Api.Endpoints.v1.Categories;
using Mype.Application.Auth.Commands.Login;
using Mype.Application.Auth.Commands.RegisterUser;
using Mype.Application.Businesses.Commands.CreateBusiness;
using Mype.Application.Businesses.Queries.GetBusinessContext;
using Mype.Application.Businesses.Queries.ListUserBusinesses;
using Mype.Application.Categories.Queries.ListCategories;
using Mype.Shared.Models;
using System.Collections.Generic;
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

            authGroup
                .MapPost(
                    "/login",
                    LoginEndpoint.DoAsync
                )
                .AllowAnonymous()
                .WithName("Login")
                .WithSummary("Iniciar sesión")
                .Produces<LoginResult>(
                    StatusCodes.Status200OK
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status400BadRequest
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status401Unauthorized
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status403Forbidden
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status500InternalServerError
                );

            #endregion

            #region Businesses

            var businessGroup = versionOneGroup
                .MapGroup("/businesses")
                .WithTags("Businesses")
                .RequireAuthorization();

            businessGroup
                .MapPost(
                    string.Empty,
                    CreateBusinessEndpoint.DoAsync
                )
                .WithName("CreateBusiness")
                .WithSummary("Crear un negocio")
                .Produces<CreateBusinessResult>(
                    StatusCodes.Status201Created
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status400BadRequest
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status401Unauthorized
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status403Forbidden
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status422UnprocessableEntity
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status500InternalServerError
                );

            businessGroup
                .MapGet(
                    string.Empty,
                    ListUserBusinessesEndpoint.DoAsync
                )
                .WithName("ListUserBusinesses")
                .WithSummary("Listar los negocios del usuario autenticado")
                .Produces<IReadOnlyCollection<BusinessSummaryResult>>(
                    StatusCodes.Status200OK
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status401Unauthorized
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status500InternalServerError
                );


            businessGroup
                .MapGet(
                    "/{businessId:guid}/context",
                    GetBusinessContextEndpoint.DoAsync
                )
                .WithName("GetBusinessContext")
                .WithSummary(
                    "Obtener el contexto autorizado de un negocio"
                )
                .Produces<BusinessContextResult>(
                    StatusCodes.Status200OK
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status401Unauthorized
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status403Forbidden
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status409Conflict
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status500InternalServerError
                );


            businessGroup
                .MapGet(
                    "/{businessId:guid}/categories",
                    ListCategoriesEndpoint.DoAsync
                )
                .WithName("ListCategories")
                .WithSummary(
                    "Consultar categorías del negocio"
                )
                .Produces<IReadOnlyCollection<CategoryListItemResult>>(
                    StatusCodes.Status200OK
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status400BadRequest
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status401Unauthorized
                )
                .Produces<HttpStatusCodeInfo>(
                    StatusCodes.Status403Forbidden
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
