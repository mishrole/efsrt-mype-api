using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.Common.Exceptions;
using Mype.Application.ProductCategories;
using Mype.Application.ProductCategories.Commands.CreateProductCategory;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.ProductCategories;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.ProductCategories
{
    public sealed class CreateProductCategoryCommandHandlerCoverageTests
    {
        [Fact]
        public async Task Handle_Should_Create_With_Neutral_Color_When_Color_Is_Missing()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Create);
            ProductCategory capturedProductCategory = null;
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.GetByNormalizedNameAsync(
                        ProductCategoryHandlerTestContext.BusinessId,
                        "BEBIDAS FRIAS",
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((ProductCategory)null);
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>())
                )
                .Callback<ProductCategory, CancellationToken>(
                    (productCategory, cancellationToken) =>
                        capturedProductCategory = productCategory
                )
                .Returns(Task.CompletedTask);
            var handler = CreateHandler(testContext);

            var result = await handler.Handle(
                CreateCommand("  Bebidas   frías  ", null),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            capturedProductCategory.Name.Should().Be("Bebidas frías");
            capturedProductCategory.NormalizedName.Should().Be("BEBIDAS FRÍAS");
            capturedProductCategory.Color.Should().Be(ProductCategoryPalette.Neutral);
        }

        [Fact]
        public async Task Handle_Should_Normalize_Provided_Color()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Create);
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.GetByNormalizedNameAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((ProductCategory)null);
            ProductCategory capturedProductCategory = null;
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>())
                )
                .Callback<ProductCategory, CancellationToken>(
                    (productCategory, cancellationToken) =>
                        capturedProductCategory = productCategory
                )
                .Returns(Task.CompletedTask);

            var result = await CreateHandler(testContext)
                .Handle(CreateCommand("Bebidas", "#abcdef"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            capturedProductCategory.Color.Should().Be("#ABCDEF");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Handle_Should_Reject_Existing_Name(bool existingCategoryIsActive)
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Create);
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.GetByNormalizedNameAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    ProductCategoryHandlerTestContext.CreateProductCategory(
                        isActive: existingCategoryIsActive
                    )
                );

            var result = await CreateHandler(testContext)
                .Handle(CreateCommand(), CancellationToken.None);

            result
                .Error.Should()
                .BeSameAs(
                    existingCategoryIsActive
                        ? ProductCategoryErrors.AlreadyExists
                        : ProductCategoryErrors.ReactivationRequired
                );
            testContext.UnitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Theory]
        [InlineData(ErrorCodes.ProductCategoryAlreadyExists)]
        [InlineData(ErrorCodes.ConcurrencyConflict)]
        [InlineData("UNKNOWN")]
        public async Task Handle_Should_Map_Persistence_Errors(string persistenceErrorCode)
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Create);
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.GetByNormalizedNameAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((ProductCategory)null);
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>())
                )
                .Returns(Task.CompletedTask);
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

            var result = await CreateHandler(testContext)
                .Handle(CreateCommand(), CancellationToken.None);

            if (persistenceErrorCode == ErrorCodes.ProductCategoryAlreadyExists)
            {
                result.Error.Should().BeSameAs(ProductCategoryErrors.AlreadyExists);
            }
            else if (persistenceErrorCode == ErrorCodes.ConcurrencyConflict)
            {
                result.Error.Should().BeSameAs(ProductCategoryErrors.ConcurrencyConflict);
            }
            else
            {
                result.Error.Should().BeSameAs(ProductCategoryErrors.PersistenceFailed);
            }
        }

        [Fact]
        public async Task Handle_Should_Return_PersistenceFailed_For_Unknown_Exception()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Create);
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.GetByNormalizedNameAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((ProductCategory)null);
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>())
                )
                .Returns(Task.CompletedTask);
            testContext
                .UnitOfWorkMock.Setup(unitOfWork =>
                    unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())
                )
                .ThrowsAsync(new InvalidOperationException());

            var result = await CreateHandler(testContext)
                .Handle(CreateCommand(), CancellationToken.None);

            result.Error.Should().BeSameAs(ProductCategoryErrors.PersistenceFailed);
        }

        [Fact]
        public async Task Handle_Should_Propagate_Cancellation()
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(ProductCategoryPermissions.Create);
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.GetByNormalizedNameAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((ProductCategory)null);
            testContext
                .ProductCategoryRepositoryMock.Setup(repository =>
                    repository.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>())
                )
                .Returns(Task.CompletedTask);
            testContext
                .UnitOfWorkMock.Setup(unitOfWork =>
                    unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())
                )
                .ThrowsAsync(new OperationCanceledException());

            await FluentActions
                .Invoking(() =>
                    CreateHandler(testContext).Handle(CreateCommand(), CancellationToken.None)
                )
                .Should()
                .ThrowAsync<OperationCanceledException>();
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public async Task Handle_Should_Reject_Invalid_Membership_Or_Role(
            bool membershipIsActive,
            bool roleIsActive
        )
        {
            var testContext = new ProductCategoryHandlerTestContext();
            testContext.SetupAccess(
                ProductCategoryPermissions.Create,
                membershipStatus: membershipIsActive
                    ? BusinessMembershipStatus.Active
                    : BusinessMembershipStatus.Inactive,
                roleIsActive: roleIsActive
            );

            var result = await CreateHandler(testContext)
                .Handle(CreateCommand(), CancellationToken.None);

            result.Error.Should().BeSameAs(ProductCategoryErrors.BusinessAccessForbidden);
        }

        private static CreateProductCategoryCommandHandler CreateHandler(
            ProductCategoryHandlerTestContext testContext
        )
        {
            return new CreateProductCategoryCommandHandler(
                testContext.MembershipRepositoryMock.Object,
                testContext.PermissionRepositoryMock.Object,
                testContext.ProductCategoryRepositoryMock.Object,
                testContext.UnitOfWorkMock.Object,
                testContext.ClockMock.Object
            );
        }

        private static CreateProductCategoryCommand CreateCommand(
            string name = "Bebidas",
            string color = "#3F6FC4"
        )
        {
            return new CreateProductCategoryCommand
            {
                BusinessId = ProductCategoryHandlerTestContext.BusinessId,
                CurrentUserId = ProductCategoryHandlerTestContext.CurrentUserId,
                Name = name,
                Color = color,
            };
        }
    }
}
