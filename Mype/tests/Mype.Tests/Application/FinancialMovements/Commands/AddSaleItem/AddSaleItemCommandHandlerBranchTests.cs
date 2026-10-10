using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Categories.Interfaces;
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

namespace Mype.Tests.Application.FinancialMovements.Commands.AddSaleItem
{
    public sealed class AddSaleItemCommandHandlerBranchTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid();
        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly Guid RoleId = Guid.NewGuid();
        private static readonly Guid CategoryId = Guid.NewGuid();
        private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

        private readonly Mock<IBusinessMembershipRepository> _memberships = new();
        private readonly Mock<IPermissionRepository> _permissions = new();
        private readonly Mock<IFinancialMovementRepository> _movements = new();
        private readonly Mock<IProductRepository> _products = new();
        private readonly Mock<ICategoryRepository> _categories = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IClock> _clock = new();

        [Fact]
        public async Task Handle_Should_Reject_Missing_Access_Context()
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
            (await Handler().Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(AddSaleItemErrors.BusinessAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Reject_Inactive_Business()
        {
            SetupAccess(BusinessStatus.Inactive);
            (await Handler().Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(AddSaleItemErrors.BusinessUnavailable);
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Permission()
        {
            SetupAccess();
            _permissions
                .Setup(x => x.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<string>());
            (await Handler().Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(AddSaleItemErrors.MovementAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Movement()
        {
            SetupAuthorized();
            _movements
                .Setup(x =>
                    x.GetAggregateTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((FinancialMovement)null);
            (await Handler().Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(AddSaleItemErrors.MovementNotFound);
        }

        [Fact]
        public async Task Handle_Should_Reject_NonSale_Movement()
        {
            SetupAuthorized();
            SetupMovement(FinancialMovementType.Expense);
            (await Handler().Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(AddSaleItemErrors.MovementMustBeSale);
        }

        [Fact]
        public async Task Handle_Should_Reject_Inactive_Product()
        {
            SetupAuthorized();
            SetupMovement();
            var product = Product.Create(
                BusinessId,
                CategoryId,
                "Producto",
                "PRODUCTO",
                1m,
                1m,
                UserId,
                Now
            );
            product.Deactivate(UserId, Now);
            _products
                .Setup(x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(product);
            (await Handler().Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(AddSaleItemErrors.ProductInactive);
        }

        [Fact]
        public async Task Handle_Should_Reject_Unavailable_Category()
        {
            SetupAuthorized();
            SetupMovement();
            var product = ActiveProduct();
            _products
                .Setup(x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(product);
            _categories
                .Setup(x =>
                    x.GetByIdAndBusinessAsync(CategoryId, BusinessId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync((Category)null);
            (await Handler().Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(AddSaleItemErrors.ProductCategoryUnavailable);
        }

        [Fact]
        public async Task Handle_Should_Map_Unexpected_Persistence_Error()
        {
            SetupSuccessfulDependencies();
            _unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());
            (await Handler().Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(AddSaleItemErrors.OperationFailed);
        }

        [Fact]
        public async Task Handle_Should_Propagate_Cancellation()
        {
            SetupSuccessfulDependencies();
            _unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            var action = () => Handler().Handle(Command(), new CancellationToken(true));
            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        private AddSaleItemCommandHandler Handler() =>
            new(
                _memberships.Object,
                _permissions.Object,
                _movements.Object,
                _products.Object,
                _categories.Object,
                _unitOfWork.Object,
                _clock.Object
            );

        private static AddSaleItemCommand Command() =>
            new()
            {
                BusinessId = BusinessId,
                MovementId = Guid.NewGuid(),
                CurrentUserId = UserId,
                ProductId = Guid.NewGuid(),
                Quantity = 2m,
                UnitAmount = 5m,
                MovementVersion = 1,
            };

        private void SetupAccess(BusinessStatus status = BusinessStatus.Active) =>
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
                        "Negocio",
                        "PEN",
                        status,
                        Guid.NewGuid(),
                        BusinessMembershipStatus.Active,
                        RoleId,
                        "OWNER",
                        true
                    )
                );

        private void SetupAuthorized()
        {
            SetupAccess();
            _permissions
                .Setup(x => x.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { SystemPermissions.MovementUpdate.Code });
        }

        private FinancialMovement SetupMovement(
            FinancialMovementType type = FinancialMovementType.Sale
        )
        {
            var movement = FinancialMovement.CreateDraft(
                BusinessId,
                type,
                new DateOnly(2026, 10, 10),
                null,
                "PEN",
                UserId,
                Now
            );
            _movements
                .Setup(x =>
                    x.GetAggregateTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(movement);
            return movement;
        }

        private static Product ActiveProduct() =>
            Product.Create(BusinessId, CategoryId, "Producto", "PRODUCTO", 5m, 2m, UserId, Now);

        private void SetupSuccessfulDependencies()
        {
            SetupAuthorized();
            SetupMovement();
            var product = ActiveProduct();
            _products
                .Setup(x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(product);
            _categories
                .Setup(x =>
                    x.GetByIdAndBusinessAsync(CategoryId, BusinessId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(
                    Category.CreateDefault(
                        BusinessId,
                        CategoryType.Sale,
                        "Productos",
                        "PRODUCTOS",
                        UserId,
                        Now
                    )
                );
            _clock.SetupGet(x => x.UtcNow).Returns(Now);
        }
    }
}
