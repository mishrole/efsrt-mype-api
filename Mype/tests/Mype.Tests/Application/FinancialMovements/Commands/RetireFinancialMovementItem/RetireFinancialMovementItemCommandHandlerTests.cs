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
using Mype.Domain.Products;

namespace Mype.Tests.Application.FinancialMovements.Commands.RetireFinancialMovementItem
{
    public class RetireFinancialMovementItemCommandHandlerTests
    {
        [Fact]
        public async Task Handle_Should_Retire_Item_And_Return_Specific_Result()
        {
            var b = Guid.NewGuid();
            var u = Guid.NewGuid();
            var role = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var m = FinancialMovement.CreateDraft(
                b,
                FinancialMovementType.Sale,
                new DateOnly(2026, 10, 10),
                null,
                "PEN",
                u,
                now
            );
            var item = m.AddSaleItem(
                Product.Create(b, Guid.NewGuid(), "A", "A", 1m, 1m, u, now),
                2m,
                3m,
                u,
                now
            );
            var memberships = new Mock<IBusinessMembershipRepository>();
            memberships
                .Setup(x => x.GetContextByBusinessAndUserAsync(b, u, It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new BusinessContextProjection(
                        b,
                        "B",
                        "PEN",
                        BusinessStatus.Active,
                        Guid.NewGuid(),
                        BusinessMembershipStatus.Active,
                        role,
                        "CUSTOM",
                        true
                    )
                );
            var permissions = new Mock<IPermissionRepository>();
            permissions
                .Setup(x => x.ListActiveCodesByRoleIdAsync(role, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { SystemPermissions.MovementUpdate.Code });
            var movements = new Mock<IFinancialMovementRepository>();
            movements
                .Setup(x =>
                    x.GetAggregateTrackedByIdAndBusinessAsync(
                        m.Id,
                        b,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(m);
            var unit = new Mock<IUnitOfWork>();
            unit.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
            var clock = new Mock<IClock>();
            clock.SetupGet(x => x.UtcNow).Returns(now.AddMinutes(1));
            var handler = new RetireFinancialMovementItemCommandHandler(
                memberships.Object,
                permissions.Object,
                movements.Object,
                unit.Object,
                clock.Object
            );
            var result = await handler.Handle(
                new()
                {
                    BusinessId = b,
                    MovementId = m.Id,
                    ItemId = item.Id,
                    CurrentUserId = u,
                    MovementVersion = 5,
                    ItemVersion = 4,
                },
                CancellationToken.None
            );
            result.IsSuccess.Should().BeTrue();
            result.Value.IsActive.Should().BeFalse();
            result.Value.MovementTotal.Should().Be(0m);
            movements.Verify(x => x.SetOriginalVersions(m, 5, item, 4), Times.Once);
        }
    }
}
