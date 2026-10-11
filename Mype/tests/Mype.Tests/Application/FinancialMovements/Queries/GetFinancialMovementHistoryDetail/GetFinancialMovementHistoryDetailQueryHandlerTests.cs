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
        public async Task Handle_Should_Disable_Writes_For_Draft_In_Inactive_Business()
        {
            var fixture = new Fixture(
                BusinessStatus.Inactive,
                [
                    SystemPermissions.HistoryRead.Code,
                    SystemPermissions.MovementUpdate.Code,
                    SystemPermissions.MovementConfirm.Code,
                    SystemPermissions.MovementDiscard.Code,
                ]
            );
            fixture.UseDetail(FinancialMovementType.Expense, FinancialMovementStatus.Draft, []);

            var result = await fixture.Handler().Handle(fixture.Query(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.IsEditable.Should().BeFalse();
            result.Value.AllowedActions.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_Should_Enable_Draft_Actions_For_Active_Business()
        {
            var fixture = new Fixture(
                BusinessStatus.Active,
                [
                    SystemPermissions.HistoryRead.Code,
                    SystemPermissions.MovementUpdate.Code,
                    SystemPermissions.MovementConfirm.Code,
                    SystemPermissions.MovementDiscard.Code,
                ]
            );
            fixture.UseDetail(FinancialMovementType.Expense, FinancialMovementStatus.Draft, []);

            var result = await fixture.Handler().Handle(fixture.Query(), CancellationToken.None);

            result.Value.IsEditable.Should().BeTrue();
            result.Value.AllowedActions.Should().BeEquivalentTo(["UPDATE", "CONFIRM", "DISCARD"]);
        }

        [Fact]
        public async Task Handle_Should_Calculate_Sale_Cost_And_Ignore_Retired_Items()
        {
            var fixture = new Fixture(BusinessStatus.Active);
            fixture.UseDetail(
                FinancialMovementType.Sale,
                FinancialMovementStatus.Confirmed,
                [fixture.Item(3m, 2m, true), fixture.Item(5m, 3m, false)]
            );

            var result = await fixture.Handler().Handle(fixture.Query(), CancellationToken.None);

            result.Value.EstimatedCost.Should().Be(6m);
            result.Value.EstimatedMargin.Should().Be(6m);
            result.Value.IsEditable.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Should_Return_Cancel_Action_Only_For_Confirmed_Movement_With_Permission()
        {
            var fixture = new Fixture(
                BusinessStatus.Active,
                [SystemPermissions.HistoryRead.Code, SystemPermissions.MovementCancel.Code]
            );
            fixture.UseDetail(FinancialMovementType.Sale, FinancialMovementStatus.Confirmed, []);

            var result = await fixture.Handler().Handle(fixture.Query(), CancellationToken.None);

            result.Value.AllowedActions.Should().Equal("CANCEL");
        }

        [Theory]
        [InlineData(FinancialMovementStatus.Cancelled)]
        [InlineData(FinancialMovementStatus.Discarded)]
        public async Task Handle_Should_Return_No_Actions_For_Terminal_Statuses(
            FinancialMovementStatus status
        )
        {
            var fixture = new Fixture(
                BusinessStatus.Active,
                [SystemPermissions.HistoryRead.Code, SystemPermissions.MovementCancel.Code]
            );
            fixture.UseDetail(FinancialMovementType.Expense, status, []);

            var result = await fixture.Handler().Handle(fixture.Query(), CancellationToken.None);

            result.Value.AllowedActions.Should().BeEmpty();
            result.Value.IsEditable.Should().BeFalse();
            result.Value.EstimatedCost.Should().BeNull();
            result.Value.EstimatedMargin.Should().BeNull();
            if (status == FinancialMovementStatus.Cancelled)
            {
                result.Value.ConfirmedAt.Should().Be(fixture.Now);
                result.Value.CancelledAt.Should().Be(fixture.Now);
                result.Value.CancelledByUserId.Should().Be(fixture.UserId);
                result.Value.CancellationReason.Should().Be("Corrección");
            }
            else
            {
                result.Value.DiscardedAt.Should().Be(fixture.Now);
                result
                    .Value.Evidences.Should()
                    .ContainSingle(evidence =>
                        evidence.Status == MovementEvidenceStatus.PendingPurge
                    );
            }
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_For_Movement_Outside_Business()
        {
            var fixture = new Fixture(BusinessStatus.Active);
            fixture
                .Movements.Setup(repository =>
                    repository.GetHistoryDetailByIdAndBusinessAsync(
                        fixture.MovementId,
                        fixture.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((FinancialMovementHistoryDetailProjection)null);

            var result = await fixture.Handler().Handle(fixture.Query(), CancellationToken.None);

            result.Error.Code.Should().Be(ErrorCodes.MovementNotFound);
        }

        [Fact]
        public async Task Handle_Should_Map_Unexpected_Error_And_Rethrow_Cancellation()
        {
            var failure = new Fixture(BusinessStatus.Active);
            failure
                .Movements.Setup(repository =>
                    repository.GetHistoryDetailByIdAndBusinessAsync(
                        failure.MovementId,
                        failure.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ThrowsAsync(new InvalidOperationException());

            var failed = await failure.Handler().Handle(failure.Query(), CancellationToken.None);
            failed.Error.Code.Should().Be(ErrorCodes.MovementDetailQueryFailed);

            var cancelled = new Fixture(BusinessStatus.Active);
            cancelled
                .Movements.Setup(repository =>
                    repository.GetHistoryDetailByIdAndBusinessAsync(
                        cancelled.MovementId,
                        cancelled.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ThrowsAsync(new OperationCanceledException());
            var action = () =>
                cancelled.Handler().Handle(cancelled.Query(), CancellationToken.None);

            await action.Should().ThrowAsync<OperationCanceledException>();
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

            public Fixture(BusinessStatus businessStatus, string[] permissions = null)
            {
                Memberships
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
                            "Business",
                            "PEN",
                            businessStatus,
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
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(permissions ?? [SystemPermissions.HistoryRead.Code]);
            }

            public FinancialMovementHistoryItemDetailProjection Item(
                decimal quantity,
                decimal unitCost,
                bool isActive
            ) =>
                new(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Categoría",
                    true,
                    Guid.NewGuid(),
                    "Producto",
                    true,
                    isActive ? "Activo" : "Retirado",
                    quantity,
                    4m,
                    unitCost,
                    quantity * 4m,
                    isActive,
                    isActive ? null : Now,
                    1
                );

            public void UseDetail(
                FinancialMovementType type,
                FinancialMovementStatus status,
                FinancialMovementHistoryItemDetailProjection[] items
            )
            {
                Movements
                    .Setup(repository =>
                        repository.GetHistoryDetailByIdAndBusinessAsync(
                            MovementId,
                            BusinessId,
                            It.IsAny<CancellationToken>()
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
                            status
                                is FinancialMovementStatus.Confirmed
                                    or FinancialMovementStatus.Cancelled
                                ? UserId
                                : null,
                            status == FinancialMovementStatus.Cancelled ? UserId : null,
                            status == FinancialMovementStatus.Cancelled ? "Corrección" : null,
                            Now,
                            Now,
                            status
                                is FinancialMovementStatus.Confirmed
                                    or FinancialMovementStatus.Cancelled
                                ? Now
                                : null,
                            status == FinancialMovementStatus.Cancelled ? Now : null,
                            status == FinancialMovementStatus.Discarded ? Now : null,
                            1,
                            items,
                            [
                                new(
                                    Guid.NewGuid(),
                                    "evidence.pdf",
                                    "application/pdf",
                                    10,
                                    status == FinancialMovementStatus.Discarded
                                        ? MovementEvidenceStatus.PendingPurge
                                        : MovementEvidenceStatus.Active,
                                    Now,
                                    1
                                ),
                            ]
                        )
                    );
            }

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
