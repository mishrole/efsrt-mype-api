using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.FinancialMovements.Models;
using Mype.Application.FinancialMovements.Queries.GetFinancialMovementHistoryDetail;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.MovementEvidences;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.FinancialMovements.Queries.GetFinancialMovementHistoryDetail
{
    public sealed class GetFinancialMovementHistoryDetailQueryHandlerTests
    {
        [Fact]
        public async Task Handle_Should_Calculate_Sale_Cost_And_Margin_Using_Active_Items()
        {
            var fixture = new Fixture();
            fixture.UseDetail(
                FinancialMovementType.Sale,
                FinancialMovementStatus.Confirmed,
                [
                    new(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "Venta",
                        true,
                        Guid.NewGuid(),
                        "Producto",
                        false,
                        "Histórico",
                        3m,
                        4m,
                        2m,
                        12m,
                        true,
                        null,
                        1
                    ),
                    new(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "Venta",
                        true,
                        Guid.NewGuid(),
                        "Producto",
                        true,
                        "Retirado",
                        5m,
                        4m,
                        3m,
                        20m,
                        false,
                        fixture.Now,
                        1
                    ),
                ]
            );

            var result = await fixture.Handler().Handle(fixture.Query(), default);

            result.IsSuccess.Should().BeTrue();
            result.Value.EstimatedCost.Should().Be(6m);
            result.Value.EstimatedMargin.Should().Be(6m);
            result.Value.Items.Should().HaveCount(2);
            result.Value.IsEditable.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Should_Return_Null_Costs_And_Draft_Actions_For_Expense()
        {
            var fixture = new Fixture([
                SystemPermissions.HistoryRead.Code,
                SystemPermissions.MovementUpdate.Code,
                SystemPermissions.MovementConfirm.Code,
                SystemPermissions.MovementDiscard.Code,
            ]);
            fixture.UseDetail(FinancialMovementType.Expense, FinancialMovementStatus.Draft, []);

            var result = await fixture.Handler().Handle(fixture.Query(), default);

            result.Value.EstimatedCost.Should().BeNull();
            result.Value.EstimatedMargin.Should().BeNull();
            result.Value.IsEditable.Should().BeTrue();
            result.Value.AllowedActions.Should().BeEquivalentTo(["UPDATE", "CONFIRM", "DISCARD"]);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_Without_Revealing_Other_Business()
        {
            var fixture = new Fixture();
            fixture
                .Movements.Setup(repository =>
                    repository.GetHistoryDetailByIdAndBusinessAsync(
                        fixture.MovementId,
                        fixture.BusinessId,
                        It.IsAny<System.Threading.CancellationToken>()
                    )
                )
                .ReturnsAsync((FinancialMovementHistoryDetailProjection)null);

            var result = await fixture.Handler().Handle(fixture.Query(), default);

            result.Error.Code.Should().Be(ErrorCodes.MovementNotFound);
        }

        private sealed class Fixture
        {
            public Guid BusinessId { get; } = Guid.NewGuid();
            public Guid MovementId { get; } = Guid.NewGuid();
            public Guid UserId { get; } = Guid.NewGuid();
            public Guid RoleId { get; } = Guid.NewGuid();
            public DateTimeOffset Now { get; } = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);
            public Mock<IBusinessMembershipRepository> Memberships { get; } = new();
            public Mock<IPermissionRepository> Permissions { get; } = new();
            public Mock<IFinancialMovementRepository> Movements { get; } = new();

            public Fixture(string[] permissions = null)
            {
                Memberships
                    .Setup(repository =>
                        repository.GetContextByBusinessAndUserAsync(
                            BusinessId,
                            UserId,
                            It.IsAny<System.Threading.CancellationToken>()
                        )
                    )
                    .ReturnsAsync(
                        new BusinessContextProjection(
                            BusinessId,
                            "Business",
                            "PEN",
                            BusinessStatus.Inactive,
                            Guid.NewGuid(),
                            BusinessMembershipStatus.Active,
                            RoleId,
                            "OWNER",
                            true
                        )
                    );
                Permissions
                    .Setup(repository =>
                        repository.ListActiveCodesByRoleIdAsync(
                            RoleId,
                            It.IsAny<System.Threading.CancellationToken>()
                        )
                    )
                    .ReturnsAsync(permissions ?? [SystemPermissions.HistoryRead.Code]);
            }

            public void UseDetail(
                FinancialMovementType type,
                FinancialMovementStatus status,
                FinancialMovementHistoryItemDetailProjection[] items
            ) =>
                Movements
                    .Setup(repository =>
                        repository.GetHistoryDetailByIdAndBusinessAsync(
                            MovementId,
                            BusinessId,
                            It.IsAny<System.Threading.CancellationToken>()
                        )
                    )
                    .ReturnsAsync(
                        new FinancialMovementHistoryDetailProjection(
                            MovementId,
                            BusinessId,
                            type,
                            status,
                            new DateOnly(2026, 10, 11),
                            "Detalle",
                            "PEN",
                            12m,
                            UserId,
                            UserId,
                            status == FinancialMovementStatus.Confirmed ? UserId : null,
                            null,
                            null,
                            Now,
                            Now,
                            status == FinancialMovementStatus.Confirmed ? Now : null,
                            null,
                            null,
                            1,
                            items,
                            [
                                new(
                                    Guid.NewGuid(),
                                    "evidence.pdf",
                                    "application/pdf",
                                    10,
                                    MovementEvidenceStatus.Active,
                                    Now,
                                    1
                                ),
                            ]
                        )
                    );

            public GetFinancialMovementHistoryDetailQuery Query() =>
                new()
                {
                    BusinessId = BusinessId,
                    MovementId = MovementId,
                    CurrentUserId = UserId,
                };

            public GetFinancialMovementHistoryDetailQueryHandler Handler() =>
                new(Memberships.Object, Permissions.Object, Movements.Object);
        }
    }
}
