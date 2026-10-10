using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Application.Products.Commands.UpdateProduct;
using Mype.Application.Products.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Categories;
using Mype.Domain.Permissions.Constants;
using Mype.Domain.Products;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.Products.Commands.UpdateProduct
{
    public class UpdateProductCommandHandlerTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid();

        private static readonly Guid UserId = Guid.NewGuid();

        private static readonly Guid RoleId = Guid.NewGuid();

        private static readonly Guid CategoryId = Guid.NewGuid();

        private static readonly Guid ProductId = Guid.NewGuid();

        private static readonly DateTimeOffset UtcNow = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

        private readonly Mock<IBusinessMembershipRepository> _memberships = new();
        private readonly Mock<IPermissionRepository> _permissions = new();
        private readonly Mock<ICategoryRepository> _categories = new();
        private readonly Mock<IProductRepository> _products = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IClock> _clock = new();
        private readonly UpdateProductCommandHandler _handler;

        public UpdateProductCommandHandlerTests()
        {
            _handler = new(
                _memberships.Object,
                _permissions.Object,
                _categories.Object,
                _products.Object,
                _unitOfWork.Object,
                _clock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Update_Product_When_Request_Is_Valid()
        {
            var product = CreateProductEntity();
            SetupValidAccess();
            _products
                .Setup(x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        ProductId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(product);
            _categories
                .Setup(x =>
                    x.GetByIdAndBusinessAsync(CategoryId, BusinessId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(CreateCategory());
            _products
                .Setup(x =>
                    x.ExistsOtherByBusinessAndNormalizedNameAsync(
                        BusinessId,
                        "AGUA 500 ML",
                        ProductId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(false);
            _clock.SetupGet(x => x.UtcNow).Returns(UtcNow.AddHours(1));
            _unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _handler.Handle(
                CreateCommand("  Agua   500 ml  "),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            product.Name.Should().Be("Agua 500 ml");
            product.NormalizedName.Should().Be("AGUA 500 ML");
            product.UpdatedByUserId.Should().Be(UserId);
            _products.Verify(x => x.SetOriginalVersion(product, 7), Times.Once);
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public async Task Handle_Should_Return_BusinessAccessForbidden_For_Invalid_Context(
            bool membershipActive,
            bool roleActive
        )
        {
            var status = membershipActive
                ? BusinessMembershipStatus.Active
                : BusinessMembershipStatus.Inactive;
            _memberships
                .Setup(x =>
                    x.GetContextByBusinessAndUserAsync(
                        BusinessId,
                        UserId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(CreateContext(membershipStatus: status, roleIsActive: roleActive));

            var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            result.Error.Should().BeSameAs(UpdateProductErrors.BusinessAccessForbidden);
            _permissions.Verify(
                x =>
                    x.ListActiveCodesByRoleIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Return_BusinessAccessForbidden_When_Context_Is_Missing()
        {
            _memberships
                .Setup(x =>
                    x.GetContextByBusinessAndUserAsync(
                        BusinessId,
                        UserId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((BusinessContextProjection)null);
            (await _handler.Handle(CreateCommand(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateProductErrors.BusinessAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Return_BusinessUnavailable_When_Business_Is_Inactive()
        {
            _memberships
                .Setup(x =>
                    x.GetContextByBusinessAndUserAsync(
                        BusinessId,
                        UserId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(CreateContext(BusinessStatus.Inactive));
            (await _handler.Handle(CreateCommand(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateProductErrors.BusinessUnavailable);
        }

        [Fact]
        public async Task Handle_Should_Return_ProductAccessForbidden_When_Permission_Is_Missing()
        {
            SetupContext();
            _permissions
                .Setup(x => x.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<string>());
            (await _handler.Handle(CreateCommand(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateProductErrors.ProductAccessForbidden);
            _products.Verify(
                x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Return_ProductNotFound_When_Product_Does_Not_Exist()
        {
            SetupValidAccess();
            _products
                .Setup(x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        ProductId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((Product)null);
            (await _handler.Handle(CreateCommand(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateProductErrors.ProductNotFound);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task Handle_Should_Reject_Invalid_Category(int caseId)
        {
            SetupValidAccess();
            _products
                .Setup(x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        ProductId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(CreateProductEntity());
            Category category = caseId switch
            {
                0 => null,
                1 => CreateCategory(CategoryType.Expense),
                _ => CreateCategory(isActive: false),
            };
            _categories
                .Setup(x =>
                    x.GetByIdAndBusinessAsync(CategoryId, BusinessId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(category);

            var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            result
                .Error.Should()
                .BeSameAs(
                    caseId switch
                    {
                        0 => UpdateProductErrors.CategoryNotFound,
                        1 => UpdateProductErrors.ProductCategoryMustBeSale,
                        _ => UpdateProductErrors.CategoryUnavailable,
                    }
                );
        }

        [Fact]
        public async Task Handle_Should_Return_ProductAlreadyExists_When_Duplicate_Exists()
        {
            SetupProductAndCategory();
            _products
                .Setup(x =>
                    x.ExistsOtherByBusinessAndNormalizedNameAsync(
                        BusinessId,
                        "GASEOSA",
                        ProductId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(true);
            (await _handler.Handle(CreateCommand(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateProductErrors.ProductAlreadyExists);
        }

        [Theory]
        [InlineData(ErrorCodes.ProductAlreadyExists)]
        [InlineData(ErrorCodes.ConcurrencyConflict)]
        public async Task Handle_Should_Map_Translated_Persistence_Errors(string code)
        {
            SetupSuccessfulUpdate();
            _unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new ApplicationErrorException(code, "error", ApplicationErrorType.Conflict)
                );

            var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

            result
                .Error.Should()
                .BeSameAs(
                    code == ErrorCodes.ProductAlreadyExists
                        ? UpdateProductErrors.ProductAlreadyExists
                        : UpdateProductErrors.ProductConcurrencyConflict
                );
        }

        [Fact]
        public async Task Handle_Should_Return_UpdateFailed_For_Unknown_Error()
        {
            SetupSuccessfulUpdate();
            _unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());
            (await _handler.Handle(CreateCommand(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateProductErrors.ProductUpdateFailed);
        }

        [Fact]
        public async Task Handle_Should_Propagate_Cancellation()
        {
            SetupSuccessfulUpdate();
            _unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            await FluentActions
                .Invoking(() => _handler.Handle(CreateCommand(), CancellationToken.None))
                .Should()
                .ThrowAsync<OperationCanceledException>();
        }

        private void SetupContext() =>
            _memberships
                .Setup(x =>
                    x.GetContextByBusinessAndUserAsync(
                        BusinessId,
                        UserId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(CreateContext());

        private void SetupValidAccess()
        {
            SetupContext();
            _permissions
                .Setup(x => x.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { SystemPermissions.ProductUpdate.Code });
        }

        private void SetupProductAndCategory()
        {
            SetupValidAccess();
            _products
                .Setup(x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        ProductId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(CreateProductEntity());
            _categories
                .Setup(x =>
                    x.GetByIdAndBusinessAsync(CategoryId, BusinessId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(CreateCategory());
        }

        private void SetupSuccessfulUpdate()
        {
            SetupProductAndCategory();
            _products
                .Setup(x =>
                    x.ExistsOtherByBusinessAndNormalizedNameAsync(
                        BusinessId,
                        "GASEOSA",
                        ProductId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(false);
            _clock.SetupGet(x => x.UtcNow).Returns(UtcNow.AddHours(1));
        }

        private static UpdateProductCommand CreateCommand(string name = "Gaseosa") =>
            new()
            {
                BusinessId = BusinessId,
                ProductId = ProductId,
                CurrentUserId = UserId,
                CategoryId = CategoryId,
                Name = name,
                SalePrice = 4m,
                UnitCost = 2.5m,
                Version = 7,
            };

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
                category.Deactivate(UserId, UtcNow.AddMinutes(1));
            }

            return category;
        }

        private static Product CreateProductEntity(bool isActive = true)
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
                product.Deactivate(UserId, UtcNow.AddMinutes(1));
            }

            return product;
        }
    }
}
