using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Mype.Api.Endpoints.v1.Auth;
using Mype.Api.Endpoints.v1.Businesses;
using Mype.Api.Endpoints.v1.Categories;
using Mype.Api.Endpoints.v1.FinancialDashboard;
using Mype.Api.Endpoints.v1.FinancialMovements;
using Mype.Api.Endpoints.v1.ProductCategories;
using Mype.Api.Endpoints.v1.Products;
using Mype.Application.Auth.Commands.Login;
using Mype.Application.Auth.Commands.RefreshSession;
using Mype.Application.Auth.Commands.RegisterUser;
using Mype.Application.Businesses.Commands.CreateBusiness;
using Mype.Application.Businesses.Commands.ReplaceOpeningBalance;
using Mype.Application.Businesses.Queries.GetBusinessContext;
using Mype.Application.Businesses.Queries.ListUserBusinesses;
using Mype.Application.Categories.Queries.ListCategories;
using Mype.Application.FinancialDashboard.Queries.GetFinancialDashboard;
using Mype.Application.FinancialMovements.Commands.CancelFinancialMovement;
using Mype.Application.FinancialMovements.Commands.ConfirmFinancialMovement;
using Mype.Application.FinancialMovements.Commands.CreateFinancialMovement;
using Mype.Application.FinancialMovements.Commands.DiscardFinancialMovement;
using Mype.Application.FinancialMovements.Commands.UpdateFinancialMovementDraft;
using Mype.Application.FinancialMovements.Models;
using Mype.Application.FinancialMovements.Queries.GetFinancialMovementDraft;
using Mype.Application.FinancialMovements.Queries.GetFinancialMovementHistoryDetail;
using Mype.Application.FinancialMovements.Queries.ListFinancialMovementDraftItems;
using Mype.Application.FinancialMovements.Queries.ListFinancialMovementDrafts;
using Mype.Application.FinancialMovements.Queries.ListFinancialMovementHistory;
using Mype.Application.MovementEvidences.Models;
using Mype.Application.ProductCategories;
using Mype.Application.Products.Commands.Common;
using Mype.Application.Products.Commands.CreateProduct;
using Mype.Application.Products.Queries.GetProductDetail;
using Mype.Application.Products.Queries.ListProducts;
using Mype.Shared.Models;

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
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
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

            authGroup
                .MapPost("/refresh", RefreshSessionEndpoint.DoAsync)
                .AllowAnonymous()
                .WithName("RefreshSession")
                .WithSummary("Renovar sesión")
                .Produces<RefreshSessionResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            authGroup
                .MapPost("/logout", LogoutEndpoint.DoAsync)
                .AllowAnonymous()
                .WithName("Logout")
                .WithSummary("Cerrar sesión")
                .Produces(StatusCodes.Status204NoContent);

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
                .MapPost(
                    "/{businessId:guid}/opening-balance/replace",
                    ReplaceOpeningBalanceEndpoint.DoAsync
                )
                .WithName("ReplaceOpeningBalance")
                .WithSummary("Corregir el saldo de apertura")
                .Produces<ReplaceOpeningBalanceResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict);
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
                .MapGet("/{businessId:guid}/dashboard", GetFinancialDashboardEndpoint.DoAsync)
                .WithName("GetFinancialDashboard")
                .WithSummary("Consultar dashboard financiero por periodo")
                .Produces<FinancialDashboardResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            #endregion

            #region Categories

            var categoryGroup = businessGroup
                .MapGroup("/{businessId:guid}/categories")
                .WithTags("Categories");

            categoryGroup
                .MapGet(string.Empty, ListCategoriesEndpoint.DoAsync)
                .WithName("ListCategories")
                .WithSummary("Consultar categorías del negocio")
                .Produces<IReadOnlyCollection<CategoryListItemResult>>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            #endregion

            #region Product Categories

            var productCategoryGroup = businessGroup
                .MapGroup("/{businessId:guid}/product-categories")
                .WithTags("ProductCategories");

            productCategoryGroup
                .MapPost(string.Empty, CreateProductCategoryEndpoint.DoAsync)
                .WithName("CreateProductCategory")
                .WithSummary("Crear una categoría de producto")
                .Produces<ProductCategoryResult>(StatusCodes.Status201Created)
                .ProducesValidationProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            productCategoryGroup
                .MapGet(string.Empty, ListProductCategoriesEndpoint.DoAsync)
                .WithName("ListProductCategories")
                .WithSummary("Listar categorías de producto")
                .Produces<IReadOnlyCollection<ProductCategoryResult>>(StatusCodes.Status200OK)
                .ProducesValidationProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status409Conflict);

            productCategoryGroup
                .MapGet("/{productCategoryId:guid}", GetProductCategoryDetailEndpoint.DoAsync)
                .WithName("GetProductCategoryDetail")
                .WithSummary("Consultar una categoría de producto")
                .Produces<ProductCategoryResult>(StatusCodes.Status200OK)
                .ProducesValidationProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict);

            productCategoryGroup
                .MapPut("/{productCategoryId:guid}", UpdateProductCategoryEndpoint.DoAsync)
                .WithName("UpdateProductCategory")
                .WithSummary("Actualizar una categoría de producto")
                .Produces<ProductCategoryResult>(StatusCodes.Status200OK)
                .ProducesValidationProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            productCategoryGroup
                .MapPatch(
                    "/{productCategoryId:guid}/deactivate",
                    DeactivateProductCategoryEndpoint.DoAsync
                )
                .WithName("DeactivateProductCategory")
                .WithSummary("Desactivar una categoría de producto")
                .Produces<ProductCategoryResult>(StatusCodes.Status200OK)
                .ProducesValidationProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            productCategoryGroup
                .MapPatch(
                    "/{productCategoryId:guid}/reactivate",
                    ReactivateProductCategoryEndpoint.DoAsync
                )
                .WithName("ReactivateProductCategory")
                .WithSummary("Reactivar una categoría de producto")
                .Produces<ProductCategoryResult>(StatusCodes.Status200OK)
                .ProducesValidationProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            #endregion

            #region Products

            var productGroup = businessGroup
                .MapGroup("/{businessId:guid}/products")
                .WithTags("Products");

            productGroup
                .MapPost(string.Empty, CreateProductEndpoint.DoAsync)
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

            productGroup
                .MapGet(string.Empty, ListProductsEndpoint.DoAsync)
                .WithName("ListProducts")
                .WithSummary("Listar y buscar productos")
                .Produces<IReadOnlyCollection<ProductListItemResult>>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            productGroup
                .MapGet("/{productId:guid}", GetProductDetailEndpoint.DoAsync)
                .WithName("GetProductDetail")
                .WithSummary("Consultar un producto")
                .Produces<ProductDetailResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            productGroup
                .MapPut("/{productId:guid}", UpdateProductEndpoint.DoAsync)
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

            productGroup
                .MapPatch("/{productId:guid}/deactivate", DeactivateProductEndpoint.DoAsync)
                .WithName("DeactivateProduct")
                .WithSummary("Desactivar un producto")
                .Produces<ProductMaintenanceResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            productGroup
                .MapPatch("/{productId:guid}/reactivate", ReactivateProductEndpoint.DoAsync)
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

            #endregion

            #region Financial Movements

            var financialMovementGroup = businessGroup
                .MapGroup("/{businessId:guid}/financial-movements")
                .WithTags("FinancialMovements");

            financialMovementGroup
                .MapPost(string.Empty, CreateFinancialMovementEndpoint.DoAsync)
                .WithName("CreateFinancialMovement")
                .WithSummary("Crear un movimiento financiero en borrador")
                .Produces<CreateFinancialMovementResult>(StatusCodes.Status201Created)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            financialMovementGroup
                .MapPut("/{movementId:guid}", UpdateFinancialMovementDraftEndpoint.DoAsync)
                .WithName("UpdateFinancialMovementDraft")
                .WithSummary("Actualizar la cabecera de un borrador")
                .Produces<UpdateFinancialMovementDraftResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            financialMovementGroup
                .MapGet("/history", ListFinancialMovementHistoryEndpoint.DoAsync)
                .WithName("ListFinancialMovementHistory")
                .WithSummary("Consultar y filtrar el historial financiero")
                .Produces<FinancialMovementHistoryPageResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);
            financialMovementGroup
                .MapGet(
                    "/{movementId:guid}/detail",
                    GetFinancialMovementHistoryDetailEndpoint.DoAsync
                )
                .WithName("GetFinancialMovementHistoryDetail")
                .WithSummary("Consultar el detalle histórico de un movimiento financiero")
                .Produces<FinancialMovementHistoryDetailResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            financialMovementGroup
                .MapGet("/{movementId:guid}/items", ListFinancialMovementDraftItemsEndpoint.DoAsync)
                .WithName("ListFinancialMovementDraftItems")
                .WithSummary("Listar los ítems activos de un borrador")
                .Produces<FinancialMovementDraftItemsResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            financialMovementGroup
                .MapGet("/{movementId:guid}", GetFinancialMovementDraftEndpoint.DoAsync)
                .WithName("GetFinancialMovementDraft")
                .WithSummary("Consultar un borrador financiero")
                .Produces<FinancialMovementDraftDetailResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            financialMovementGroup
                .MapGet(string.Empty, ListFinancialMovementDraftsEndpoint.DoAsync)
                .WithName("ListFinancialMovementDrafts")
                .WithSummary("Listar borradores financieros")
                .Produces<IReadOnlyCollection<FinancialMovementDraftListItemResult>>(
                    StatusCodes.Status200OK
                )
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            financialMovementGroup
                .MapPost("/{movementId:guid}/confirm", ConfirmFinancialMovementEndpoint.DoAsync)
                .WithName("ConfirmFinancialMovement")
                .WithSummary("Confirmar un movimiento financiero")
                .Produces<ConfirmFinancialMovementResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status422UnprocessableEntity)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            financialMovementGroup
                .MapPost("/{movementId:guid}/cancel", CancelFinancialMovementEndpoint.DoAsync)
                .WithName("CancelFinancialMovement")
                .WithSummary("Anular un movimiento financiero confirmado")
                .Produces<CancelFinancialMovementResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);
            financialMovementGroup
                .MapPost("/{movementId:guid}/discard", DiscardFinancialMovementEndpoint.DoAsync)
                .WithName("DiscardFinancialMovement")
                .WithSummary("Descartar un movimiento financiero en borrador")
                .Produces<DiscardFinancialMovementResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            #endregion

            #region Financial Movement Items

            var financialMovementItemGroup = financialMovementGroup
                .MapGroup("/{movementId:guid}")
                .WithTags("FinancialMovementItems");

            financialMovementItemGroup
                .MapPost("/sale-items", AddSaleItemEndpoint.DoAsync)
                .WithName("AddSaleItem")
                .WithSummary("Agregar un producto a una venta")
                .Produces<FinancialMovementItemMaintenanceResult>(StatusCodes.Status201Created)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status422UnprocessableEntity)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            financialMovementItemGroup
                .MapPut("/sale-items/{itemId:guid}", UpdateSaleItemEndpoint.DoAsync)
                .WithName("UpdateSaleItem")
                .WithSummary("Modificar un producto de una venta")
                .Produces<FinancialMovementItemMaintenanceResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status422UnprocessableEntity)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            financialMovementItemGroup
                .MapPost("/expense-items", AddExpenseItemEndpoint.DoAsync)
                .WithName("AddExpenseItem")
                .WithSummary("Agregar un concepto a un gasto")
                .Produces<ExpenseMovementItemMaintenanceResult>(StatusCodes.Status201Created)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status422UnprocessableEntity)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            financialMovementItemGroup
                .MapPut("/expense-items/{itemId:guid}", UpdateExpenseItemEndpoint.DoAsync)
                .WithName("UpdateExpenseItem")
                .WithSummary("Modificar un concepto de gasto")
                .Produces<ExpenseMovementItemMaintenanceResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status422UnprocessableEntity)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            financialMovementItemGroup
                .MapPatch(
                    "/items/{itemId:guid}/retire",
                    RetireFinancialMovementItemEndpoint.DoAsync
                )
                .WithName("RetireFinancialMovementItem")
                .WithSummary("Retirar un ítem del movimiento")
                .Produces<RetiredFinancialMovementItemResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status401Unauthorized)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            #endregion

            #region Evidences

            var evidenceGroup = financialMovementGroup
                .MapGroup("/{movementId:guid}/evidences")
                .WithTags("MovementEvidences");

            evidenceGroup
                .MapPost(string.Empty, AttachMovementEvidenceEndpoint.DoAsync)
                .WithName("AttachMovementEvidence")
                .WithSummary("Adjuntar evidencia a un movimiento")
                .Produces<MovementEvidenceResult>(StatusCodes.Status201Created)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status413PayloadTooLarge)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status422UnprocessableEntity)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status502BadGateway)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            evidenceGroup
                .MapPut("/{evidenceId:guid}", ReplaceMovementEvidenceEndpoint.DoAsync)
                .WithName("ReplaceMovementEvidence")
                .WithSummary("Sustituir una evidencia activa")
                .Produces<MovementEvidenceResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status413PayloadTooLarge)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status422UnprocessableEntity)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status502BadGateway)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            evidenceGroup
                .MapPatch("/{evidenceId:guid}/remove", RemoveMovementEvidenceEndpoint.DoAsync)
                .WithName("RemoveMovementEvidence")
                .WithSummary("Retirar lógicamente una evidencia")
                .Produces(StatusCodes.Status204NoContent)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status400BadRequest)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            evidenceGroup
                .MapGet(string.Empty, ListMovementEvidencesEndpoint.DoAsync)
                .WithName("ListMovementEvidences")
                .WithSummary("Listar evidencias activas de un movimiento")
                .Produces<IReadOnlyCollection<MovementEvidenceResult>>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            evidenceGroup
                .MapGet("/{evidenceId:guid}/content", OpenMovementEvidenceEndpoint.DoAsync)
                .WithName("OpenMovementEvidence")
                .WithSummary("Obtener acceso temporal a una evidencia")
                .Produces<TemporaryEvidenceAccessResult>(StatusCodes.Status200OK)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status403Forbidden)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status404NotFound)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status409Conflict)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status502BadGateway)
                .Produces<HttpStatusCodeInfo>(StatusCodes.Status500InternalServerError);

            #endregion
        }
    }
}
