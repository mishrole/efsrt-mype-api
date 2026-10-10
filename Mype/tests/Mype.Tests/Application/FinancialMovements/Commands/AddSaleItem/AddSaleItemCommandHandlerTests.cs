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
using Mype.Application.FinancialMovements.Commands.AddSaleItem;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Application.Products.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Categories;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;
using Mype.Domain.Products;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.FinancialMovements.Commands.AddSaleItem
{
    public class AddSaleItemCommandHandlerTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid(),
            MovementId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            ProductId = Guid.NewGuid();
        private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        private readonly Mock<IBusinessMembershipRepository> _memberships = new();
        private readonly Mock<IPermissionRepository> _permissions = new();
        private readonly Mock<IFinancialMovementRepository> _movements = new();
        private readonly Mock<IProductRepository> _products = new();
        private readonly Mock<ICategoryRepository> _categories = new();
        private readonly Mock<IUnitOfWork> _unit = new();
        private readonly Mock<IClock> _clock = new();

        [Fact]
        public async Task Handle_Should_Add_Item_And_Return_Calculations()
        {
            var movement = Movement();
            SetupAccess();
            _movements
                .Setup(x =>
                    x.GetAggregateTrackedByIdAndBusinessAsync(
                        MovementId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(movement);
            _products
                .Setup(x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        ProductId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(CreateProduct());
            _categories
                .Setup(x =>
                    x.GetByIdAndBusinessAsync(CategoryId, BusinessId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(CreateCategory());
            _clock.SetupGet(x => x.UtcNow).Returns(Now);
            _unit.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
            var result = await Handler().Handle(Command(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Value.Item.SubtotalAmount.Should().Be(35m);
            result.Value.Item.EstimatedCost.Should().Be(22m);
            result.Value.MovementTotal.Should().Be(35m);
            _movements.Verify(x => x.SetOriginalVersion(movement, 7), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Reject_Product_From_Another_Business_As_NotFound()
        {
            SetupAccess();
            _movements
                .Setup(x =>
                    x.GetAggregateTrackedByIdAndBusinessAsync(
                        MovementId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(Movement());
            _products
                .Setup(x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        ProductId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((Product)null);
            (await Handler().Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(AddSaleItemErrors.ProductNotFound);
        }

        [Fact]
        public async Task Handle_Should_Map_Concurrency()
        {
            SetupSuccess();
            _unit
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new ApplicationErrorException(
                        ErrorCodes.ConcurrencyConflict,
                        "conflict",
                        ApplicationErrorType.Conflict
                    )
                );
            (await Handler().Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(AddSaleItemErrors.ConcurrencyConflict);
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
                .ReturnsAsync(new[] { SystemPermissions.MovementUpdate.Code });
        }

        private void SetupSuccess()
        {
            SetupAccess();
            _movements
                .Setup(x =>
                    x.GetAggregateTrackedByIdAndBusinessAsync(
                        MovementId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(Movement());
            _products
                .Setup(x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        ProductId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(CreateProduct());
            _categories
                .Setup(x =>
                    x.GetByIdAndBusinessAsync(CategoryId, BusinessId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(CreateCategory());
            _clock.SetupGet(x => x.UtcNow).Returns(Now);
        }

        private AddSaleItemCommandHandler Handler() =>
            new(
                _memberships.Object,
                _permissions.Object,
                _movements.Object,
                _products.Object,
                _categories.Object,
                _unit.Object,
                _clock.Object
            );

        private static AddSaleItemCommand Command() =>
            new()
            {
                BusinessId = BusinessId,
                MovementId = MovementId,
                CurrentUserId = UserId,
                ProductId = ProductId,
                Quantity = 10m,
                UnitAmount = 3.5m,
                MovementVersion = 7,
            };

        private static FinancialMovement Movement()
        {
            var m = FinancialMovement.CreateDraft(
                BusinessId,
                FinancialMovementType.Sale,
                new DateOnly(2026, 10, 10),
                null,
                "PEN",
                UserId,
                Now
            );
            typeof(Mype.Domain.Common.Entity)
                .GetProperty(nameof(Mype.Domain.Common.Entity.Id))
                .SetValue(m, MovementId);
            return m;
        }

        private static Product CreateProduct()
        {
            var p = Product.Create(
                BusinessId,
                CategoryId,
                "Gaseosa",
                "GASEOSA",
                3.5m,
                2.2m,
                UserId,
                Now
            );
            typeof(Mype.Domain.Common.Entity)
                .GetProperty(nameof(Mype.Domain.Common.Entity.Id))
                .SetValue(p, ProductId);
            return p;
        }

        private static Category CreateCategory() =>
            Category.CreateDefault(
                BusinessId,
                CategoryType.Sale,
                "Productos",
                "PRODUCTOS",
                UserId,
                Now
            );
    }
}
