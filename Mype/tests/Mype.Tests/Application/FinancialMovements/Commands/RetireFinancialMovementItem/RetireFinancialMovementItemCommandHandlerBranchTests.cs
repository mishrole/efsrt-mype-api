using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialMovements.Commands.RetireFinancialMovementItem;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;
using Mype.Domain.Products;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.FinancialMovements.Commands.RetireFinancialMovementItem
{
    public sealed class RetireFinancialMovementItemCommandHandlerBranchTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid();
        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly Guid RoleId = Guid.NewGuid();
        private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
        private readonly Mock<IBusinessMembershipRepository> _memberships = new();
        private readonly Mock<IPermissionRepository> _permissions = new();
        private readonly Mock<IFinancialMovementRepository> _movements = new();
        private readonly Mock<IUnitOfWork> _unit = new();
        private readonly Mock<IClock> _clock = new();

        [Fact]
        public async Task Handle_Should_Reject_Missing_Item()
        {
            var command = Setup(false);
            (await Handler().Handle(command, CancellationToken.None))
                .Error.Should()
                .BeSameAs(RetireFinancialMovementItemErrors.ItemNotFound);
        }

        [Fact]
        public async Task Handle_Should_Reject_Retired_Item()
        {
            var command = Setup();
            var movement = await _movements.Object.GetAggregateTrackedByIdAndBusinessAsync(
                command.MovementId,
                BusinessId,
                CancellationToken.None
            );
            movement.RetireItem(command.ItemId, UserId, Now);
            (await Handler().Handle(command, CancellationToken.None))
                .Error.Should()
                .BeSameAs(RetireFinancialMovementItemErrors.ItemAlreadyRetired);
        }

        [Fact]
        public async Task Handle_Should_Map_Concurrency()
        {
            var command = Setup();
            _unit
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new ApplicationErrorException(
                        ErrorCodes.ConcurrencyConflict,
                        "conflict",
                        ApplicationErrorType.Conflict
                    )
                );
            (await Handler().Handle(command, CancellationToken.None))
                .Error.Should()
                .BeSameAs(RetireFinancialMovementItemErrors.ConcurrencyConflict);
        }

        [Fact]
        public async Task Handle_Should_Map_Unexpected_Error()
        {
            var command = Setup();
            _unit
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());
            (await Handler().Handle(command, CancellationToken.None))
                .Error.Should()
                .BeSameAs(RetireFinancialMovementItemErrors.OperationFailed);
        }

        [Fact]
        public async Task Handle_Should_Propagate_Cancellation()
        {
            var command = Setup();
            _unit
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            var action = () => Handler().Handle(command, new CancellationToken(true));
            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        private RetireFinancialMovementItemCommandHandler Handler() =>
            new(
                _memberships.Object,
                _permissions.Object,
                _movements.Object,
                _unit.Object,
                _clock.Object
            );

        private RetireFinancialMovementItemCommand Setup(bool itemExists = true)
        {
            var movement = FinancialMovement.CreateDraft(
                BusinessId,
                FinancialMovementType.Sale,
                new DateOnly(2026, 10, 10),
                null,
                "PEN",
                UserId,
                Now
            );
            var product = Product.Create(
                BusinessId,
                Guid.NewGuid(),
                "Producto",
                "PRODUCTO",
                5m,
                2m,
                UserId,
                Now
            );
            var itemId = itemExists
                ? movement.AddSaleItem(product, 1m, 5m, UserId, Now).Id
                : Guid.NewGuid();
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
            _clock.SetupGet(x => x.UtcNow).Returns(Now.AddMinutes(1));
            return new RetireFinancialMovementItemCommand
            {
                BusinessId = BusinessId,
                MovementId = movement.Id,
                ItemId = itemId,
                CurrentUserId = UserId,
                MovementVersion = 1,
                ItemVersion = 1,
            };
        }
    }
}
