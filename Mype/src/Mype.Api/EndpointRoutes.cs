using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Mype.Api.Endpoints.v1.Auth;
using Mype.Api.Endpoints.v1.Businesses;
using Mype.Api.Endpoints.v1.Categories;
using Mype.Api.Endpoints.v1.FinancialMovements;
using Mype.Api.Endpoints.v1.Products;
using Mype.Application.Auth.Commands.Login;
using Mype.Application.Auth.Commands.RegisterUser;
using Mype.Application.Businesses.Commands.CreateBusiness;
using Mype.Application.Businesses.Queries.GetBusinessContext;
using Mype.Application.Businesses.Queries.ListUserBusinesses;
using Mype.Application.Categories.Queries.ListCategories;
using Mype.Application.FinancialMovements.Commands.CreateFinancialMovement;
using Mype.Application.FinancialMovements.Commands.UpdateFinancialMovementDraft;
using Mype.Application.FinancialMovements.Models;
using Mype.Application.FinancialMovements.Queries.GetFinancialMovementDraft;
using Mype.Application.FinancialMovements.Queries.ListFinancialMovementDrafts;
using Mype.Application.Products.Commands.Common;
using Mype.Application.Products.Commands.CreateProduct;
using Mype.Application.Products.Queries.GetProductDetail;
using Mype.Application.Products.Queries.ListProducts;
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

            var authGroup = versionOneGroup.MapGroup("/auth").WithTags("Auth");

            authGroup
                .MapPost("/register", RegisterUserEndpoint.DoAsync)
                .AllowAnonymous()
                .WithName("RegisterUser")
                .WithSummary("Registrar usuario")
                .Produces<RegisterUserResult>(StatusCodes.Status201Created)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            authGroup
                .MapPost("/login", LoginEndpoint.DoAsync)
                .AllowAnonymous()
                .WithName("Login")
                .WithSummary("Iniciar sesión")
                .Produces<LoginResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            #endregion

            #region Businesses

            var businessGroup = versionOneGroup
                .MapGroup("/businesses")
                .WithTags("Businesses")
                .RequireAuthorization();

            businessGroup
                .MapPost(string.Empty, CreateBusinessEndpoint.DoAsync)
                .WithName("CreateBusiness")
                .WithSummary("Crear un negocio")
                .Produces<CreateBusinessResult>(StatusCodes.Status201Created)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status422UnprocessableEntity)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            businessGroup
                .MapGet(string.Empty, ListUserBusinessesEndpoint.DoAsync)
                .WithName("ListUserBusinesses")
                .WithSummary("Listar los negocios del usuario autenticado")
                .Produces<IReadOnlyCollection<BusinessSummaryResult>>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            businessGroup
                .MapGet("/{businessId:guid}/context", GetBusinessContextEndpoint.DoAsync)
                .WithName("GetBusinessContext")
                .WithSummary("Obtener el contexto autorizado de un negocio")
                .Produces<BusinessContextResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            businessGroup
                .MapGet("/{businessId:guid}/categories", ListCategoriesEndpoint.DoAsync)
                .WithName("ListCategories")
                .WithSummary("Consultar categorías del negocio")
                .Produces<IReadOnlyCollection<CategoryListItemResult>>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            businessGroup
                .MapPost("/{businessId:guid}/products", CreateProductEndpoint.DoAsync)
                .WithName("CreateProduct")
                .WithSummary("Crear un producto")
                .Produces<CreateProductResult>(StatusCodes.Status201Created)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status422UnprocessableEntity)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            businessGroup
                .MapGet("/{businessId:guid}/products", ListProductsEndpoint.DoAsync)
                .WithName("ListProducts")
                .WithSummary("Listar y buscar productos")
                .Produces<IReadOnlyCollection<ProductListItemResult>>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            businessGroup
                .MapGet(
                    "/{businessId:guid}/products/{productId:guid}",
                    GetProductDetailEndpoint.DoAsync
                )
                .WithName("GetProductDetail")
                .WithSummary("Consultar un producto")
                .Produces<ProductDetailResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            businessGroup
                .MapPut(
                    "/{businessId:guid}/products/{productId:guid}",
                    UpdateProductEndpoint.DoAsync
                )
                .WithName("UpdateProduct")
                .WithSummary("Actualizar un producto")
                .Produces<ProductMaintenanceResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status422UnprocessableEntity)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            businessGroup
                .MapPatch(
                    "/{businessId:guid}/products/{productId:guid}/deactivate",
                    DeactivateProductEndpoint.DoAsync
                )
                .WithName("DeactivateProduct")
                .WithSummary("Desactivar un producto")
                .Produces<ProductMaintenanceResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            businessGroup
                .MapPatch(
                    "/{businessId:guid}/products/{productId:guid}/reactivate",
                    ReactivateProductEndpoint.DoAsync
                )
                .WithName("ReactivateProduct")
                .WithSummary("Reactivar un producto")
                .Produces<ProductMaintenanceResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status422UnprocessableEntity)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            businessGroup
                .MapPost(
                    "/{businessId:guid}/financial-movements",
                    CreateFinancialMovementEndpoint.DoAsync
                )
                .WithName("CreateFinancialMovement")
                .WithSummary("Crear un movimiento financiero en borrador")
                .Produces<CreateFinancialMovementResult>(StatusCodes.Status201Created)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            businessGroup
                .MapPut(
                    "/{businessId:guid}/financial-movements/{movementId:guid}",
                    UpdateFinancialMovementDraftEndpoint.DoAsync
                )
                .WithName("UpdateFinancialMovementDraft")
                .WithSummary("Actualizar la cabecera de un borrador")
                .Produces<UpdateFinancialMovementDraftResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            businessGroup
                .MapGet(
                    "/{businessId:guid}/financial-movements/{movementId:guid}",
                    GetFinancialMovementDraftEndpoint.DoAsync
                )
                .WithName("GetFinancialMovementDraft")
                .WithSummary("Consultar un borrador financiero")
                .Produces<FinancialMovementDraftDetailResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            businessGroup
                .MapGet(
                    "/{businessId:guid}/financial-movements",
                    ListFinancialMovementDraftsEndpoint.DoAsync
                )
                .WithName("ListFinancialMovementDrafts")
                .WithSummary("Listar borradores financieros")
                .Produces<IReadOnlyCollection<FinancialMovementDraftListItemResult>>(
                    StatusCodes.Status200OK
                )
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            businessGroup.MapPost("/{businessId:guid}/financial-movements/{movementId:guid}/sale-items", AddSaleItemEndpoint.DoAsync).WithName("AddSaleItem").Produces<FinancialMovementItemMaintenanceResult>(StatusCodes.Status201Created).Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest).Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden).Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound).Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict).Produces<HttpStatusCodeInfo>(StatusCodes.Status422UnprocessableEntity).Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);
            businessGroup.MapPut("/{businessId:guid}/financial-movements/{movementId:guid}/sale-items/{itemId:guid}", UpdateSaleItemEndpoint.DoAsync).WithName("UpdateSaleItem").Produces<FinancialMovementItemMaintenanceResult>(StatusCodes.Status200OK).Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest).Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden).Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound).Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict).Produces<HttpStatusCodeInfo>(StatusCodes.Status422UnprocessableEntity).Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);
            businessGroup.MapPatch("/{businessId:guid}/financial-movements/{movementId:guid}/items/{itemId:guid}/retire", RetireFinancialMovementItemEndpoint.DoAsync).WithName("RetireFinancialMovementItem").Produces<RetiredFinancialMovementItemResult>(StatusCodes.Status200OK).Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest).Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden).Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound).Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict).Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);
            #endregion
        }
    }
}


