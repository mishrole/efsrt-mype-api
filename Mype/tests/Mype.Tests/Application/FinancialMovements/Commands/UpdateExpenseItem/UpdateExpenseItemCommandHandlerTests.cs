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
using Mype.Application.FinancialMovements.Commands.UpdateExpenseItem;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Categories;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.FinancialMovements.Commands.UpdateExpenseItem
{
    public sealed class UpdateExpenseItemCommandHandlerTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid();
        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly Guid RoleId = Guid.NewGuid();
        private static readonly Guid CategoryId = Guid.NewGuid();
        private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
        private readonly Mock<IBusinessMembershipRepository> _memberships = new();
        private readonly Mock<IPermissionRepository> _permissions = new();
        private readonly Mock<IFinancialMovementRepository> _movements = new();
        private readonly Mock<ICategoryRepository> _categories = new();
        private readonly Mock<IUnitOfWork> _unit = new();
        private readonly Mock<IClock> _clock = new();

        [Fact]
        public async Task Handle_Should_Update_Expense_Item_And_Set_Both_Versions()
        {
            var setup = Setup();
            _unit.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

            var result = await Handler().Handle(setup.Command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Item.Description.Should().Be("Bolsas reforzadas");
            result.Value.Item.CategoryId.Should().Be(setup.Category.Id);
            result.Value.Item.CategoryName.Should().Be(setup.Category.Name);
            result.Value.Item.SubtotalAmount.Should().Be(46.50m);
            result.Value.MovementTotal.Should().Be(46.50m);
            _movements.Verify(
                x => x.SetOriginalVersions(setup.Movement, 9, setup.Item, 8),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Item()
        {
            var setup = Setup(false);
            (await Handler().Handle(setup.Command, CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateExpenseItemErrors.ItemNotFound);
        }

        [Fact]
        public async Task Handle_Should_Reject_Retired_Item()
        {
            var setup = Setup();
            setup.Movement.RetireItem(setup.Item.Id, UserId, Now);
            (await Handler().Handle(setup.Command, CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateExpenseItemErrors.ItemAlreadyRetired);
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Category()
        {
            var setup = Setup();

            _categories
                .Setup(repository =>
                    repository.GetByIdAndBusinessAsync(
                        setup.Command.CategoryId,
                        setup.Command.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((Category)null);

            var result = await Handler().Handle(setup.Command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeSameAs(UpdateExpenseItemErrors.CategoryNotFound);

            _movements.Verify(
                repository =>
                    repository.SetOriginalVersions(
                        It.IsAny<FinancialMovement>(),
                        It.IsAny<uint>(),
                        It.IsAny<FinancialMovementItem>(),
                        It.IsAny<uint>()
                    ),
                Times.Never
            );

            _unit.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Map_Concurrency()
        {
            var setup = Setup();
            _unit
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new ApplicationErrorException(
                        ErrorCodes.ConcurrencyConflict,
                        "conflict",
                        ApplicationErrorType.Conflict
                    )
                );
            (await Handler().Handle(setup.Command, CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateExpenseItemErrors.ConcurrencyConflict);
        }

        [Fact]
        public async Task Handle_Should_Map_Unexpected_Error()
        {
            var setup = Setup();
            _unit
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());
            (await Handler().Handle(setup.Command, CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateExpenseItemErrors.OperationFailed);
        }

        [Fact]
        public async Task Handle_Should_Propagate_Cancellation()
        {
            var setup = Setup();
            _unit
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            var action = () => Handler().Handle(setup.Command, new CancellationToken(true));
            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        private UpdateExpenseItemCommandHandler Handler() =>
            new(
                _memberships.Object,
                _permissions.Object,
                _movements.Object,
                _categories.Object,
                _unit.Object,
                _clock.Object
            );

        private SetupData Setup(bool itemExists = true)
        {
            var category = Category.CreateDefault(
                BusinessId,
                CategoryType.Expense,
                "Insumos",
                "INSUMOS",
                UserId,
                Now
            );
            var movement = FinancialMovement.CreateDraft(
                BusinessId,
                FinancialMovementType.Expense,
                new DateOnly(2026, 10, 10),
                null,
                "PEN",
                UserId,
                Now
            );
            var item = movement.AddExpenseItem(category.Id, "Inicial", 2m, 10m, UserId, Now);
            var itemId = itemExists ? item.Id : Guid.NewGuid();
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
                        BusinessStatus.Active,
                        Guid.NewGuid(),
                        BusinessMembershipStatus.Active,
                        RoleId,
                        "OWNER",
                        true
                    )
                );
            _permissions
                .Setup(x => x.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { SystemPermissions.MovementUpdate.Code });
            _movements
                .Setup(x =>
                    x.GetAggregateTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(movement);
            _categories
                .Setup(x =>
                    x.GetByIdAndBusinessAsync(
                        category.Id,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(category);
            _clock.SetupGet(x => x.UtcNow).Returns(Now.AddMinutes(1));
            var command = new UpdateExpenseItemCommand
            {
                BusinessId = BusinessId,
                MovementId = movement.Id,
                ItemId = itemId,
                CurrentUserId = UserId,
                CategoryId = category.Id,
                Description = "Bolsas reforzadas",
                Quantity = 3m,
                UnitAmount = 15.50m,
                MovementVersion = 9,
                ItemVersion = 8,
            };
            return new SetupData(movement, item, category, command);
        }

        private sealed record SetupData(
            FinancialMovement Movement,
            FinancialMovementItem Item,
            Category Category,
            UpdateExpenseItemCommand Command
        );
    }
}
