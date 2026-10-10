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
using Mype.Application.FinancialMovements.Commands.AddExpenseItem;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Categories;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.FinancialMovements.Commands.AddExpenseItem
{
    public sealed class AddExpenseItemCommandHandlerTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid();
        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly Guid RoleId = Guid.NewGuid();
        private static readonly Guid CategoryId = Guid.NewGuid();
        private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

        private readonly Mock<IBusinessMembershipRepository> _memberships = new();
        private readonly Mock<IPermissionRepository> _permissions = new();
        private readonly Mock<IFinancialMovementRepository> _movements = new();
        private readonly Mock<ICategoryRepository> _categories = new();
        private readonly Mock<IUnitOfWork> _unit = new();
        private readonly Mock<IClock> _clock = new();

        [Fact]
        public async Task Handle_Should_Add_Expense_Item_And_Return_Result()
        {
            var category = CreateExpenseCategory();
            var movement = SetupSuccess(category);

            _unit
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);

            var result = await Handler().Handle(Command(category.Id), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Item.Description.Should().Be("Bolsas");
            result.Value.Item.CategoryId.Should().Be(category.Id);
            result.Value.Item.CategoryName.Should().Be(category.Name);
            result.Value.Item.SubtotalAmount.Should().Be(31m);
            result.Value.MovementTotal.Should().Be(31m);

            _movements.Verify(repository => repository.SetOriginalVersion(movement, 7), Times.Once);

            _unit.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Access()
        {
            _memberships
                .Setup(repository =>
                    repository.GetContextByBusinessAndUserAsync(
                        BusinessId,
                        UserId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((BusinessContextProjection)null);

            var result = await Handler().Handle(Command(), CancellationToken.None);

            result.Error.Should().BeSameAs(AddExpenseItemErrors.BusinessAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Reject_Inactive_Business()
        {
            SetupAccess(BusinessStatus.Inactive);

            var result = await Handler().Handle(Command(), CancellationToken.None);

            result.Error.Should().BeSameAs(AddExpenseItemErrors.BusinessUnavailable);
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Permission()
        {
            SetupAccess();
            _permissions
                .Setup(repository =>
                    repository.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(Array.Empty<string>());

            var result = await Handler().Handle(Command(), CancellationToken.None);

            result.Error.Should().BeSameAs(AddExpenseItemErrors.MovementAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Movement()
        {
            SetupAuthorized();
            _movements
                .Setup(repository =>
                    repository.GetAggregateTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((FinancialMovement)null);

            var result = await Handler().Handle(Command(), CancellationToken.None);

            result.Error.Should().BeSameAs(AddExpenseItemErrors.MovementNotFound);
        }

        [Fact]
        public async Task Handle_Should_Reject_Sale_Movement()
        {
            SetupAuthorized();
            SetupMovement(FinancialMovementType.Sale);

            var result = await Handler().Handle(Command(), CancellationToken.None);

            result.Error.Should().BeSameAs(AddExpenseItemErrors.MovementMustBeExpense);
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Category()
        {
            SetupAuthorized();
            SetupMovement();
            _categories
                .Setup(repository =>
                    repository.GetByIdAndBusinessAsync(
                        CategoryId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((Category)null);

            var result = await Handler().Handle(Command(), CancellationToken.None);

            result.Error.Should().BeSameAs(AddExpenseItemErrors.CategoryNotFound);
        }

        [Fact]
        public async Task Handle_Should_Reject_Sale_Category()
        {
            SetupAuthorized();
            SetupMovement();
            _categories
                .Setup(repository =>
                    repository.GetByIdAndBusinessAsync(
                        CategoryId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    Category.CreateDefault(
                        BusinessId,
                        CategoryType.Sale,
                        "Venta",
                        "VENTA",
                        UserId,
                        Now
                    )
                );

            var result = await Handler().Handle(Command(), CancellationToken.None);

            result.Error.Should().BeSameAs(AddExpenseItemErrors.ExpenseCategoryUnavailable);
        }

        [Fact]
        public async Task Handle_Should_Map_Concurrency()
        {
            var category = CreateExpenseCategory();
            SetupSuccess(category);
            _unit
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new ApplicationErrorException(
                        ErrorCodes.ConcurrencyConflict,
                        "conflict",
                        ApplicationErrorType.Conflict
                    )
                );

            var result = await Handler().Handle(Command(category.Id), CancellationToken.None);

            result.Error.Should().BeSameAs(AddExpenseItemErrors.ConcurrencyConflict);
        }

        [Fact]
        public async Task Handle_Should_Map_Unexpected_Error()
        {
            var category = CreateExpenseCategory();
            SetupSuccess(category);
            _unit
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());

            var result = await Handler().Handle(Command(category.Id), CancellationToken.None);

            result.Error.Should().BeSameAs(AddExpenseItemErrors.OperationFailed);
        }

        [Fact]
        public async Task Handle_Should_Propagate_Cancellation()
        {
            var category = CreateExpenseCategory();
            SetupSuccess(category);
            _unit
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var action = () => Handler().Handle(Command(category.Id), new CancellationToken(true));

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        private AddExpenseItemCommandHandler Handler() =>
            new(
                _memberships.Object,
                _permissions.Object,
                _movements.Object,
                _categories.Object,
                _unit.Object,
                _clock.Object
            );

        private static AddExpenseItemCommand Command(Guid? categoryId = null) =>
            new()
            {
                BusinessId = BusinessId,
                MovementId = Guid.NewGuid(),
                CurrentUserId = UserId,
                CategoryId = categoryId ?? CategoryId,
                Description = "Bolsas",
                Quantity = 2m,
                UnitAmount = 15.50m,
                MovementVersion = 7,
            };

        private void SetupAccess(BusinessStatus status = BusinessStatus.Active) =>
            _memberships
                .Setup(repository =>
                    repository.GetContextByBusinessAndUserAsync(
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
                .Setup(repository =>
                    repository.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(new[] { SystemPermissions.MovementUpdate.Code });
        }

        private FinancialMovement SetupMovement(
            FinancialMovementType type = FinancialMovementType.Expense
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
                .Setup(repository =>
                    repository.GetAggregateTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(movement);

            return movement;
        }

        private FinancialMovement SetupSuccess(Category category)
        {
            SetupAuthorized();
            var movement = SetupMovement();

            _categories
                .Setup(repository =>
                    repository.GetByIdAndBusinessAsync(
                        category.Id,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(category);

            _clock.SetupGet(clock => clock.UtcNow).Returns(Now);

            return movement;
        }

        private static Category CreateExpenseCategory() =>
            Category.CreateDefault(
                BusinessId,
                CategoryType.Expense,
                "Insumos",
                "INSUMOS",
                UserId,
                Now
            );
    }
}
