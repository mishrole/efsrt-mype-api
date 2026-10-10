using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Application.Products.Commands.DeactivateProduct;
using Mype.Application.Products.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Categories;
using Mype.Domain.Permissions.Constants;
using Mype.Domain.Products;
using Mype.Shared.Constants;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Tests.Application.Products.Commands.DeactivateProduct
{
    public class DeactivateProductCommandHandlerTests
    {

        private static readonly Guid BusinessId =
            Guid.NewGuid();

        private static readonly Guid UserId =
            Guid.NewGuid();

        private static readonly Guid RoleId =
            Guid.NewGuid();

        private static readonly Guid CategoryId =
            Guid.NewGuid();

        private static readonly Guid ProductId =
            Guid.NewGuid();

        private static readonly DateTimeOffset UtcNow =
            new(
                2026,
                10,
                10,
                12,
                0,
                0,
                TimeSpan.Zero
            );

        private readonly Mock<IBusinessMembershipRepository> _memberships = new();
        private readonly Mock<IPermissionRepository> _permissions = new();
        private readonly Mock<IProductRepository> _products = new();
        private readonly Mock<ICategoryRepository> _categories = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IClock> _clock = new();
        private readonly DeactivateProductCommandHandler _handler;

        public DeactivateProductCommandHandlerTests() => _handler = new(_memberships.Object, _permissions.Object, _products.Object, _categories.Object, _unitOfWork.Object, _clock.Object);

        [Fact]
        public async Task Handle_Should_Change_State_When_Request_Is_Valid()
        {
            var product = CreateProductEntity(true);
            SetupValidAccess();
            _products.Setup(x => x.GetTrackedByIdAndBusinessAsync(ProductId, BusinessId, It.IsAny<CancellationToken>())).ReturnsAsync(product);
            _categories.Setup(x => x.GetByIdAndBusinessAsync(CategoryId, BusinessId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateCategory());
            _clock.SetupGet(x => x.UtcNow).Returns(UtcNow.AddHours(1));
            _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.IsActive.Should().Be(false);
            _products.Verify(x => x.SetOriginalVersion(product, 7), Times.Once);
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Return_AccessForbidden_When_Context_Is_Missing()
        {
            _memberships.Setup(x => x.GetContextByBusinessAndUserAsync(BusinessId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync((BusinessContextProjection)null);
            (await _handler.Handle(CreateCommand(), CancellationToken.None)).Error.Should().BeSameAs(DeactivateProductErrors.BusinessAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Return_AccessForbidden_When_Membership_Or_Role_Is_Inactive()
        {
            _memberships.Setup(x => x.GetContextByBusinessAndUserAsync(BusinessId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateContext(membershipStatus: BusinessMembershipStatus.Inactive, roleIsActive: false));
            (await _handler.Handle(CreateCommand(), CancellationToken.None)).Error.Should().BeSameAs(DeactivateProductErrors.BusinessAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Return_BusinessUnavailable_When_Business_Is_Inactive()
        {
            _memberships.Setup(x => x.GetContextByBusinessAndUserAsync(BusinessId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateContext(BusinessStatus.Inactive));
            (await _handler.Handle(CreateCommand(), CancellationToken.None)).Error.Should().BeSameAs(DeactivateProductErrors.BusinessUnavailable);
        }

        [Fact]
        public async Task Handle_Should_Return_ProductAccessForbidden_When_Permission_Is_Missing()
        {
            SetupContext();
            _permissions.Setup(x => x.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<string>());
            (await _handler.Handle(CreateCommand(), CancellationToken.None)).Error.Should().BeSameAs(DeactivateProductErrors.ProductAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Return_ProductNotFound_When_Product_Does_Not_Exist()
        {
            SetupValidAccess();
            _products.Setup(x => x.GetTrackedByIdAndBusinessAsync(ProductId, BusinessId, It.IsAny<CancellationToken>())).ReturnsAsync((Product)null);
            (await _handler.Handle(CreateCommand(), CancellationToken.None)).Error.Should().BeSameAs(DeactivateProductErrors.ProductNotFound);
        }

        [Fact]
        public async Task Handle_Should_Return_State_Error_When_State_Is_Unchanged()
        {
            SetupValidAccess();
            _products.Setup(x => x.GetTrackedByIdAndBusinessAsync(ProductId, BusinessId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateProductEntity(false));
            (await _handler.Handle(CreateCommand(), CancellationToken.None)).Error.Should().BeSameAs(DeactivateProductErrors.ProductAlreadyInactive);
        }

        [Fact]
        public async Task Handle_Should_Return_ConcurrencyConflict()
        {
            SetupSuccessfulChange();
            _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new ApplicationErrorException(ErrorCodes.ProductConcurrencyConflict, "error", ApplicationErrorType.Conflict));
            (await _handler.Handle(CreateCommand(), CancellationToken.None)).Error.Should().BeSameAs(DeactivateProductErrors.ProductConcurrencyConflict);
        }

        [Fact]
        public async Task Handle_Should_Return_StatusChangeFailed_For_Unknown_Error()
        {
            SetupSuccessfulChange();
            _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());
            (await _handler.Handle(CreateCommand(), CancellationToken.None)).Error.Should().BeSameAs(DeactivateProductErrors.ProductStatusChangeFailed);
        }

        [Fact]
        public async Task Handle_Should_Propagate_Cancellation()
        {
            SetupSuccessfulChange();
            _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());
            await FluentActions.Invoking(() => _handler.Handle(CreateCommand(), CancellationToken.None)).Should().ThrowAsync<OperationCanceledException>();
        }

        private void SetupContext() => _memberships.Setup(x => x.GetContextByBusinessAndUserAsync(BusinessId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateContext());
        private void SetupValidAccess() { SetupContext(); _permissions.Setup(x => x.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { SystemPermissions.ProductDeactivate.Code }); }
        private void SetupSuccessfulChange() { SetupValidAccess(); _products.Setup(x => x.GetTrackedByIdAndBusinessAsync(ProductId, BusinessId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateProductEntity(true)); _categories.Setup(x => x.GetByIdAndBusinessAsync(CategoryId, BusinessId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateCategory()); _clock.SetupGet(x => x.UtcNow).Returns(UtcNow.AddHours(1)); }
        private static DeactivateProductCommand CreateCommand() => new() { BusinessId = BusinessId, ProductId = ProductId, CurrentUserId = UserId, Version = 7 };

        private static BusinessContextProjection CreateContext(
            BusinessStatus businessStatus = BusinessStatus.Active,
            BusinessMembershipStatus membershipStatus = BusinessMembershipStatus.Active,
            bool roleIsActive = true
        )
        {
            return new BusinessContextProjection(
                BusinessId,
                "Bodega",
                "PEN",
                businessStatus,
                Guid.NewGuid(),
                membershipStatus,
                RoleId,
                "CUSTOM",
                roleIsActive
            );
        }

        private static Category CreateCategory(
            CategoryType type = CategoryType.Sale,
            bool isActive = true
        )
        {
            var category = Category.CreateDefault(
                BusinessId,
                type,
                "Productos",
                "PRODUCTOS",
                UserId,
                UtcNow
            );

            if (!isActive)
            {
                category.Deactivate(
                    UserId,
                    UtcNow.AddMinutes(1)
                );
            }

            return category;
        }

        private static Product CreateProductEntity(
            bool isActive = true
        )
        {
            var product = Product.Create(
                BusinessId,
                CategoryId,
                "Gaseosa",
                "GASEOSA",
                3.50m,
                2.20m,
                UserId,
                UtcNow
            );

            if (!isActive)
            {
                product.Deactivate(
                    UserId,
                    UtcNow.AddMinutes(1)
                );
            }

            return product;
        }

    }
}
