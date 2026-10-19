using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Moq;
using Mype.Application.ProductCategories;
using Mype.Application.ProductCategories.Queries.GetProductCategoryDetail;
using Mype.Application.ProductCategories.Queries.ListProductCategories;
using Mype.Domain.Businesses;
using Mype.Domain.ProductCategories;

namespace Mype.Tests.Application.ProductCategories
{
    public sealed class ProductCategoryQueryHandlerCoverageTests
    {
        [Fact]
        public async System.Threading.Tasks.Task List_Should_Normalize_Search_And_Map_Values()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Read);
            var productCategory = ProductCategoryHandlerTestContext.CreateProductCategory("Bebidas frías", "BEBIDAS FRÍAS");
            testContext.ProductCategoryRepositoryMock.Setup(repository => repository.ListAsync(ProductCategoryHandlerTestContext.BusinessId, "BEBIDAS FRIAS", true, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { productCategory });
            var handler = new ListProductCategoriesQueryHandler(testContext.MembershipRepositoryMock.Object, testContext.PermissionRepositoryMock.Object, testContext.ProductCategoryRepositoryMock.Object);

            var result = await handler.Handle(new ListProductCategoriesQuery { BusinessId = ProductCategoryHandlerTestContext.BusinessId, CurrentUserId = ProductCategoryHandlerTestContext.CurrentUserId, Search = "  bebidas   frias ", IsActive = true }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().ContainSingle().Which.Name.Should().Be("Bebidas frías");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("   ")]
        public async System.Threading.Tasks.Task List_Should_Treat_Empty_Search_As_Null(string search)
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Read);
            testContext.ProductCategoryRepositoryMock.Setup(repository => repository.ListAsync(ProductCategoryHandlerTestContext.BusinessId, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<ProductCategory>());
            var handler = new ListProductCategoriesQueryHandler(testContext.MembershipRepositoryMock.Object, testContext.PermissionRepositoryMock.Object, testContext.ProductCategoryRepositoryMock.Object);

            var result = await handler.Handle(new ListProductCategoriesQuery { BusinessId = ProductCategoryHandlerTestContext.BusinessId, CurrentUserId = ProductCategoryHandlerTestContext.CurrentUserId, Search = search }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async System.Threading.Tasks.Task Detail_Should_Return_Mapped_Value()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Read);
            var productCategory = ProductCategoryHandlerTestContext.CreateProductCategory();
            testContext.ProductCategoryRepositoryMock.Setup(repository => repository.GetByIdAndBusinessAsync(ProductCategoryHandlerTestContext.ProductCategoryId, ProductCategoryHandlerTestContext.BusinessId, It.IsAny<CancellationToken>())).ReturnsAsync(productCategory);
            var handler = new GetProductCategoryDetailQueryHandler(testContext.MembershipRepositoryMock.Object, testContext.PermissionRepositoryMock.Object, testContext.ProductCategoryRepositoryMock.Object);

            var result = await handler.Handle(new GetProductCategoryDetailQuery { BusinessId = ProductCategoryHandlerTestContext.BusinessId, ProductCategoryId = ProductCategoryHandlerTestContext.ProductCategoryId, CurrentUserId = ProductCategoryHandlerTestContext.CurrentUserId }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Name.Should().Be("Bebidas");
            result.Value.Color.Should().Be("#3F6FC4");
        }

        [Fact]
        public async System.Threading.Tasks.Task Detail_Should_Return_NotFound()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Read);
            testContext.ProductCategoryRepositoryMock.Setup(repository => repository.GetByIdAndBusinessAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((ProductCategory)null);
            var handler = new GetProductCategoryDetailQueryHandler(testContext.MembershipRepositoryMock.Object, testContext.PermissionRepositoryMock.Object, testContext.ProductCategoryRepositoryMock.Object);

            var result = await handler.Handle(new GetProductCategoryDetailQuery { BusinessId = ProductCategoryHandlerTestContext.BusinessId, ProductCategoryId = ProductCategoryHandlerTestContext.ProductCategoryId, CurrentUserId = ProductCategoryHandlerTestContext.CurrentUserId }, CancellationToken.None);

            result.Error.Should().BeSameAs(ProductCategoryErrors.NotFound);
        }

        [Theory]
        [InlineData(BusinessStatus.Inactive, true)]
        [InlineData(BusinessStatus.Active, false)]
        public async System.Threading.Tasks.Task List_Should_Return_Access_Error(BusinessStatus businessStatus, bool includePermission)
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Read, businessStatus: businessStatus, includePermission: includePermission);
            var handler = new ListProductCategoriesQueryHandler(testContext.MembershipRepositoryMock.Object, testContext.PermissionRepositoryMock.Object, testContext.ProductCategoryRepositoryMock.Object);

            var result = await handler.Handle(new ListProductCategoriesQuery { BusinessId = ProductCategoryHandlerTestContext.BusinessId, CurrentUserId = ProductCategoryHandlerTestContext.CurrentUserId }, CancellationToken.None);

            result.Error.Should().BeSameAs(businessStatus == BusinessStatus.Inactive ? ProductCategoryErrors.BusinessUnavailable : ProductCategoryErrors.AccessForbidden);
        }

        [Fact]
        public async System.Threading.Tasks.Task List_Should_Return_BusinessAccessForbidden_When_Context_Is_Missing()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupMissingBusinessContext();
            var handler = new ListProductCategoriesQueryHandler(testContext.MembershipRepositoryMock.Object, testContext.PermissionRepositoryMock.Object, testContext.ProductCategoryRepositoryMock.Object);

            var result = await handler.Handle(new ListProductCategoriesQuery { BusinessId = ProductCategoryHandlerTestContext.BusinessId, CurrentUserId = ProductCategoryHandlerTestContext.CurrentUserId }, CancellationToken.None);

            result.Error.Should().BeSameAs(ProductCategoryErrors.BusinessAccessForbidden);
        }
    }
}
