using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialMovements.Commands.RetireFinancialMovementItem;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;

namespace Mype.Tests.Application.FinancialMovements.Commands.RetireFinancialMovementItem
{
    public sealed class RetireExpenseItemCommandHandlerTests
    {
        [Fact]
        public async Task Handle_Should_Retire_Expense_Item_Set_Versions_And_Save_Once()
        {
            var businessId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var now = new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
            var movement = FinancialMovement.CreateDraft(
                businessId,
                FinancialMovementType.Expense,
                new DateOnly(2026, 10, 10),
                null,
                "PEN",
                userId,
                now
            );
            var item = movement.AddExpenseItem(categoryId, "Concepto", 2m, 5m, userId, now);
            var memberships = new Mock<IBusinessMembershipRepository>();
            var permissions = new Mock<IPermissionRepository>();
            var movements = new Mock<IFinancialMovementRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var clock = new Mock<IClock>();
            memberships
                .Setup(x =>
                    x.GetContextByBusinessAndUserAsync(
                        businessId,
                        userId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    new BusinessContextProjection(
                        businessId,
                        "Negocio",
                        "PEN",
                        BusinessStatus.Active,
                        Guid.NewGuid(),
                        BusinessMembershipStatus.Active,
                        roleId,
                        "OWNER",
                        true
                    )
                );
            permissions
                .Setup(x => x.ListActiveCodesByRoleIdAsync(roleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { SystemPermissions.MovementUpdate.Code });
            movements
                .Setup(x =>
                    x.GetAggregateTrackedByIdAndBusinessAsync(
                        movement.Id,
                        businessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(movement);
            unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);
            clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(1));
            var handler = new RetireFinancialMovementItemCommandHandler(
                memberships.Object,
                permissions.Object,
                movements.Object,
                unitOfWork.Object,
                clock.Object
            );
            var command = new RetireFinancialMovementItemCommand
            {
                BusinessId = businessId,
                MovementId = movement.Id,
                ItemId = item.Id,
                CurrentUserId = userId,
                MovementVersion = 7,
                ItemVersion = 6,
            };

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.ItemId.Should().Be(item.Id);
            result.Value.IsActive.Should().BeFalse();
            result.Value.RetiredAt.Should().Be(now.AddMinutes(1));
            result.Value.MovementTotal.Should().Be(0m);
            movement.Status.Should().Be(FinancialMovementStatus.Draft);
            movements.Verify(x => x.SetOriginalVersions(movement, 7, item, 6), Times.Once);
            unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
