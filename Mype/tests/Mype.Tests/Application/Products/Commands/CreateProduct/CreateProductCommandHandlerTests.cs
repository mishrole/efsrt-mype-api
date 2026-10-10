using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Common.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Application.Products.Commands.CreateProduct;
using Mype.Application.Products.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Categories;
using Mype.Domain.Permissions.Constants;
using Mype.Domain.Products;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Tests.Application.Products.Commands.CreateProduct
{
    public class CreateProductCommandHandlerTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid();
        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly Guid CategoryId = Guid.NewGuid();
        private static readonly Guid RoleId = Guid.NewGuid();
        private readonly Mock<IBusinessMembershipRepository> _memberships = new();
        private readonly Mock<IPermissionRepository> _permissions = new();
        private readonly Mock<ICategoryRepository> _categories = new();
        private readonly Mock<IProductRepository> _products = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IClock> _clock = new();
        private readonly CreateProductCommandHandler _handler;

        public CreateProductCommandHandlerTests()
        {
            _handler = new(_memberships.Object, _permissions.Object, _categories.Object, _products.Object, _unitOfWork.Object, _clock.Object);
        }

        [Fact]
        public async Task Handle_Should_Create_Product_When_Request_Is_Valid()
        {
            var now = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);
            SetupValid(now);
            Product captured = null;
            _products.Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                .Callback<Product, CancellationToken>((x, _) => captured = x).Returns(Task.CompletedTask);

            var result = await _handler.Handle(CreateCommand("  Gaseosa   500 ml  "), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            captured.Should().NotBeNull();
            captured.Name.Should().Be("Gaseosa 500 ml");
            captured.NormalizedName.Should().Be("GASEOSA 500 ML");
            result.Value.CategoryName.Should().Be("Productos");
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Return_AccessForbidden_When_Context_Is_Missing()
        {
            _memberships.Setup(x => x.GetContextByBusinessAndUserAsync(BusinessId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync((BusinessContextProjection)null);
            var result = await _handler.Handle(CreateCommand(), CancellationToken.None);
            result.Error.Should().BeSameAs(CreateProductErrors.BusinessAccessForbidden);
            _permissions.Verify(x => x.ListActiveCodesByRoleIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Return_ProductAccessForbidden_When_Permission_Is_Missing()
        {
            SetupContext();
            _permissions.Setup(x => x.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<string>());
            var result = await _handler.Handle(CreateCommand(), CancellationToken.None);
            result.Error.Should().BeSameAs(CreateProductErrors.ProductAccessForbidden);
            _categories.Verify(x => x.GetByIdAndBusinessAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Reject_Invalid_Category()
        {
            SetupContext(); SetupPermission();
            _categories.Setup(x => x.GetByIdAndBusinessAsync(CategoryId, BusinessId, It.IsAny<CancellationToken>())).ReturnsAsync((Category)null);
            (await _handler.Handle(CreateCommand(), CancellationToken.None)).Error.Should().BeSameAs(CreateProductErrors.CategoryNotFound);
        }

        [Fact]
        public async Task Handle_Should_Reject_Duplicate_Product()
        {
            SetupValid(DateTimeOffset.UtcNow);
            _products.Setup(x => x.ExistsByBusinessAndNormalizedNameAsync(BusinessId, "GASEOSA 500 ML", It.IsAny<CancellationToken>())).ReturnsAsync(true);
            (await _handler.Handle(CreateCommand(), CancellationToken.None)).Error.Should().BeSameAs(CreateProductErrors.ProductAlreadyExists);
            _products.Verify(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private void SetupValid(DateTimeOffset now)
        {
            SetupContext(); SetupPermission(); _clock.SetupGet(x => x.UtcNow).Returns(now);
            _categories.Setup(x => x.GetByIdAndBusinessAsync(CategoryId, BusinessId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Category.CreateDefault(BusinessId, CategoryType.Sale, "Productos", "PRODUCTOS", UserId, now));
            _products.Setup(x => x.ExistsByBusinessAndNormalizedNameAsync(BusinessId, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        }
        private void SetupContext() => _memberships.Setup(x => x.GetContextByBusinessAndUserAsync(BusinessId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BusinessContextProjection(BusinessId, "Bodega", "PEN", BusinessStatus.Active, Guid.NewGuid(), BusinessMembershipStatus.Active, RoleId, "CUSTOM", true));
        private void SetupPermission() => _permissions.Setup(x => x.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { SystemPermissions.ProductCreate.Code });
        private static CreateProductCommand CreateCommand(string name = "Gaseosa 500 ml") => new() { BusinessId = BusinessId, CurrentUserId = UserId, CategoryId = CategoryId, Name = name, SalePrice = 3.50m, UnitCost = 2.20m };
    }
}
