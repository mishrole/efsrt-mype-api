using System;
using System.Threading;
using FluentAssertions;
using Moq;
using Mype.Application.Common.Exceptions;
using Mype.Application.ProductCategories;
using Mype.Application.ProductCategories.Commands.DeactivateProductCategory;
using Mype.Application.ProductCategories.Commands.ReactivateProductCategory;
using Mype.Domain.ProductCategories;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.ProductCategories
{
    public sealed class ProductCategoryStatusCommandHandlerCoverageTests
    {
        [Fact]
        public async System.Threading.Tasks.Task Deactivate_Should_Persist_Valid_Transition()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Deactivate);
            var productCategory = ProductCategoryHandlerTestContext.CreateProductCategory();
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.GetTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(productCategory);

            var result = await CreateDeactivateHandler(testContext)
                .Handle(CreateDeactivateCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            productCategory.IsActive.Should().BeFalse();
            testContext.ProductCategoryRepositoryMock.Verify(
                repository => repository.SetOriginalVersion(productCategory, 9),
                Times.Once
            );
        }

        [Fact]
        public async System.Threading.Tasks.Task Deactivate_Should_Reject_Already_Inactive()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Deactivate);
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.GetTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    ProductCategoryHandlerTestContext.CreateProductCategory(isActive: false)
                );

            var result = await CreateDeactivateHandler(testContext)
                .Handle(CreateDeactivateCommand(), CancellationToken.None);

            result.Error.Should().BeSameAs(ProductCategoryErrors.AlreadyInactive);
        }

        [Fact]
        public async System.Threading.Tasks.Task Reactivate_Should_Persist_Valid_Transition()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Reactivate);
            var productCategory = ProductCategoryHandlerTestContext.CreateProductCategory(
                isActive: false
            );
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.GetTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(productCategory);

            var result = await CreateReactivateHandler(testContext)
                .Handle(CreateReactivateCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            productCategory.IsActive.Should().BeTrue();
        }

        [Fact]
        public async System.Threading.Tasks.Task Reactivate_Should_Reject_Already_Active()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Reactivate);
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.GetTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(ProductCategoryHandlerTestContext.CreateProductCategory());

            var result = await CreateReactivateHandler(testContext)
                .Handle(CreateReactivateCommand(), CancellationToken.None);

            result.Error.Should().BeSameAs(ProductCategoryErrors.AlreadyActive);
        }

        [Theory]
        [InlineData(true, ErrorCodes.ConcurrencyConflict)]
        [InlineData(false, ErrorCodes.ProductCategoryAlreadyExists)]
        [InlineData(true, "UNKNOWN")]
        public async System.Threading.Tasks.Task Status_Handlers_Should_Map_Persistence_Errors(
            bool deactivate,
            string persistenceErrorCode
        )
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(
                deactivate
                    ? ProductCategoryPermissions.Deactivate
                    : ProductCategoryPermissions.Reactivate
            );
            var productCategory = ProductCategoryHandlerTestContext.CreateProductCategory(
                isActive: deactivate
            );
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.GetTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(productCategory);
            testContext
                .UnitOfWorkMock.Setup(unitOfWork =>
                    unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())
                )
                .ThrowsAsync(
                    new ApplicationErrorException(
                        persistenceErrorCode,
                        "error",
                        ApplicationErrorType.Conflict
                    )
                );

            var result = deactivate
                ? await CreateDeactivateHandler(testContext)
                    .Handle(CreateDeactivateCommand(), CancellationToken.None)
                : await CreateReactivateHandler(testContext)
                    .Handle(CreateReactivateCommand(), CancellationToken.None);

            result
                .Error.Should()
                .BeSameAs(
                    persistenceErrorCode == ErrorCodes.ConcurrencyConflict
                        ? ProductCategoryErrors.ConcurrencyConflict
                    : persistenceErrorCode == ErrorCodes.ProductCategoryAlreadyExists
                        ? ProductCategoryErrors.AlreadyExists
                    : ProductCategoryErrors.PersistenceFailed
                );
        }

        [Fact]
        public async System.Threading.Tasks.Task Status_Handlers_Should_Return_NotFound()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Deactivate);
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.GetTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((ProductCategory)null);

            var result = await CreateDeactivateHandler(testContext)
                .Handle(CreateDeactivateCommand(), CancellationToken.None);

            result.Error.Should().BeSameAs(ProductCategoryErrors.NotFound);
        }

        [Fact]
        public async System.Threading.Tasks.Task Status_Handler_Should_Propagate_Cancellation()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Deactivate);
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.GetTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(ProductCategoryHandlerTestContext.CreateProductCategory());
            testContext
                .UnitOfWorkMock.Setup(unitOfWork =>
                    unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())
                )
                .ThrowsAsync(new OperationCanceledException());

            await FluentActions
                .Invoking(() =>
                    CreateDeactivateHandler(testContext)
                        .Handle(CreateDeactivateCommand(), CancellationToken.None)
                )
                .Should()
                .ThrowAsync<OperationCanceledException>();
        }

        private static DeactivateProductCategoryCommandHandler CreateDeactivateHandler(
            ProductCategoryHandlerTestContext testContext
        ) =>
            new(
                testContext.MembershipRepositoryMock.Object,
                testContext.PermissionRepositoryMock.Object,
                testContext.ProductCategoryRepositoryMock.Object,
                testContext.UnitOfWorkMock.Object,
                testContext.ClockMock.Object
            );

        private static ReactivateProductCategoryCommandHandler CreateReactivateHandler(
            ProductCategoryHandlerTestContext testContext
        ) =>
            new(
                testContext.MembershipRepositoryMock.Object,
                testContext.PermissionRepositoryMock.Object,
                testContext.ProductCategoryRepositoryMock.Object,
                testContext.UnitOfWorkMock.Object,
                testContext.ClockMock.Object
            );

        private static DeactivateProductCategoryCommand CreateDeactivateCommand() =>
            new()
            {
                BusinessId = ProductCategoryHandlerTestContext.BusinessId,
                ProductCategoryId = ProductCategoryHandlerTestContext.ProductCategoryId,
                CurrentUserId = ProductCategoryHandlerTestContext.CurrentUserId,
                Version = 9,
            };

        private static ReactivateProductCategoryCommand CreateReactivateCommand() =>
            new()
            {
                BusinessId = ProductCategoryHandlerTestContext.BusinessId,
                ProductCategoryId = ProductCategoryHandlerTestContext.ProductCategoryId,
                CurrentUserId = ProductCategoryHandlerTestContext.CurrentUserId,
                Version = 9,
            };
    }
}
