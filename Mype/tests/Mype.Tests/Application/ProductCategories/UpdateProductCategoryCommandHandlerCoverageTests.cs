using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.Common.Exceptions;
using Mype.Application.ProductCategories;
using Mype.Application.ProductCategories.Commands.UpdateProductCategory;
using Mype.Domain.ProductCategories;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.ProductCategories
{
    public sealed class UpdateProductCategoryCommandHandlerCoverageTests
    {
        [Fact]
        public async Task Handle_Should_Update_And_Set_Original_Version()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Update);
            var productCategory = ProductCategoryHandlerTestContext.CreateProductCategory();
            testContext.ProductCategoryRepositoryMock.Setup(repository => repository.GetTrackedByIdAndBusinessAsync(ProductCategoryHandlerTestContext.ProductCategoryId, ProductCategoryHandlerTestContext.BusinessId, It.IsAny<CancellationToken>())).ReturnsAsync(productCategory);
            testContext.ProductCategoryRepositoryMock.Setup(repository => repository.GetByNormalizedNameAsync(ProductCategoryHandlerTestContext.BusinessId, "BEBIDAS FRIAS", It.IsAny<CancellationToken>())).ReturnsAsync(productCategory);

            var result = await CreateHandler(testContext).Handle(CreateCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            productCategory.Name.Should().Be("Bebidas frías");
            productCategory.Color.Should().Be("#ABCDEF");
            testContext.ProductCategoryRepositoryMock.Verify(repository => repository.SetOriginalVersion(productCategory, 7), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Update);
            testContext.ProductCategoryRepositoryMock.Setup(repository => repository.GetTrackedByIdAndBusinessAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((ProductCategory)null);

            var result = await CreateHandler(testContext).Handle(CreateCommand(), CancellationToken.None);

            result.Error.Should().BeSameAs(ProductCategoryErrors.NotFound);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Handle_Should_Reject_Different_Duplicate(bool duplicateIsActive)
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Update);
            var productCategory = ProductCategoryHandlerTestContext.CreateProductCategory();
            var duplicateProductCategory = ProductCategoryHandlerTestContext.CreateProductCategory("Duplicada", "BEBIDAS FRIAS", duplicateIsActive);
            testContext.ProductCategoryRepositoryMock.Setup(repository => repository.GetTrackedByIdAndBusinessAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(productCategory);
            testContext.ProductCategoryRepositoryMock.Setup(repository => repository.GetByNormalizedNameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(duplicateProductCategory);

            var result = await CreateHandler(testContext).Handle(CreateCommand(), CancellationToken.None);

            result.Error.Should().BeSameAs(duplicateIsActive ? ProductCategoryErrors.AlreadyExists : ProductCategoryErrors.ReactivationRequired);
        }

        [Theory]
        [InlineData(ErrorCodes.ProductCategoryAlreadyExists)]
        [InlineData(ErrorCodes.ConcurrencyConflict)]
        [InlineData("UNKNOWN")]
        public async Task Handle_Should_Map_Persistence_Error(string persistenceErrorCode)
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Update);
            var productCategory = ProductCategoryHandlerTestContext.CreateProductCategory();
            testContext.ProductCategoryRepositoryMock.Setup(repository => repository.GetTrackedByIdAndBusinessAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(productCategory);
            testContext.ProductCategoryRepositoryMock.Setup(repository => repository.GetByNormalizedNameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ProductCategory)null);
            testContext.UnitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new ApplicationErrorException(persistenceErrorCode, "error", ApplicationErrorType.Conflict));

            var result = await CreateHandler(testContext).Handle(CreateCommand(), CancellationToken.None);

            result.Error.Should().BeSameAs(persistenceErrorCode == ErrorCodes.ProductCategoryAlreadyExists ? ProductCategoryErrors.AlreadyExists : persistenceErrorCode == ErrorCodes.ConcurrencyConflict ? ProductCategoryErrors.ConcurrencyConflict : ProductCategoryErrors.PersistenceFailed);
        }

        [Fact]
        public async Task Handle_Should_Propagate_Cancellation()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Update);
            var productCategory = ProductCategoryHandlerTestContext.CreateProductCategory();
            testContext.ProductCategoryRepositoryMock.Setup(repository => repository.GetTrackedByIdAndBusinessAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(productCategory);
            testContext.ProductCategoryRepositoryMock.Setup(repository => repository.GetByNormalizedNameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ProductCategory)null);
            testContext.UnitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());

            await FluentActions.Invoking(() => CreateHandler(testContext).Handle(CreateCommand(), CancellationToken.None)).Should().ThrowAsync<OperationCanceledException>();
        }

        private static UpdateProductCategoryCommandHandler CreateHandler(ProductCategoryHandlerTestContext testContext)
        {
            return new UpdateProductCategoryCommandHandler(testContext.MembershipRepositoryMock.Object, testContext.PermissionRepositoryMock.Object, testContext.ProductCategoryRepositoryMock.Object, testContext.UnitOfWorkMock.Object, testContext.ClockMock.Object);
        }

        private static UpdateProductCategoryCommand CreateCommand()
        {
            return new UpdateProductCategoryCommand { BusinessId = ProductCategoryHandlerTestContext.BusinessId, ProductCategoryId = ProductCategoryHandlerTestContext.ProductCategoryId, CurrentUserId = ProductCategoryHandlerTestContext.CurrentUserId, Name = "  Bebidas   frías ", Color = "#abcdef", Version = 7 };
        }
    }
}
