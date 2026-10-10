using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Permissions.Interfaces;
using Mype.Application.Products.Interfaces;
using Mype.Application.Products.Models;
using Mype.Application.Products.Queries.GetProductDetail;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Categories;
using Mype.Domain.Permissions.Constants;

namespace Mype.Tests.Application.Products.Queries.GetProductDetail
{
    public class GetProductDetailQueryHandlerTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid();
        private readonly Mock<IBusinessMembershipRepository> _memberships = new();
        private readonly Mock<IPermissionRepository> _permissions = new();
        private readonly Mock<IProductRepository> _products = new();
        private readonly GetProductDetailQueryHandler _handler;

        public GetProductDetailQueryHandlerTests() =>
            _handler = new(_memberships.Object, _permissions.Object, _products.Object);

        [Fact]
        public async Task Handle_Should_Return_Product_When_Access_Is_Valid()
        {
            SetupAccess();
            _products
                .Setup(x =>
                    x.GetByIdAndBusinessAsync(ProductId, BusinessId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(
                    new ProductDetailProjection(
                        ProductId,
                        BusinessId,
                        CategoryId,
                        "Productos",
                        CategoryType.Sale,
                        "Gaseosa",
                        3.5m,
                        2.2m,
                        true,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow,
                        1
                    )
                );
            var result = await _handler.Handle(CreateQuery(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Value.Id.Should().Be(ProductId);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Product_Does_Not_Exist()
        {
            SetupAccess();
            _products
                .Setup(x =>
                    x.GetByIdAndBusinessAsync(ProductId, BusinessId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync((ProductDetailProjection)null);
            (await _handler.Handle(CreateQuery(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(GetProductDetailErrors.ProductNotFound);
        }

        private void SetupAccess()
        {
            _memberships
                .Setup(x =>
                    x.GetContextByBusinessAndUserAsync(
                        BusinessId,
                        UserId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    new BusinessContextProjection(
                        BusinessId,
                        "Bodega",
                        "PEN",
                        BusinessStatus.Active,
                        Guid.NewGuid(),
                        BusinessMembershipStatus.Active,
                        RoleId,
                        "CUSTOM",
                        true
                    )
                );
            _permissions
                .Setup(x => x.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { SystemPermissions.ProductRead.Code });
        }

        private static GetProductDetailQuery CreateQuery() =>
            new()
            {
                BusinessId = BusinessId,
                ProductId = ProductId,
                CurrentUserId = UserId,
            };
    }
}
