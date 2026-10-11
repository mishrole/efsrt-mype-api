using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialMovements.Commands.CancelFinancialMovement;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.FinancialMovements.Constraints;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.FinancialMovements.Commands.CancelFinancialMovement
{
    public sealed class CancelFinancialMovementCommandHandlerTests
    {
        [Fact]
        public async Task Handle_Should_Normalize_Cancel_Set_Version_And_Save_Once()
        {
            var fixture = new Fixture();
            var movement = fixture.ConfirmedMovement();
            fixture.UseMovement(movement);
            var command = fixture.Command("  Registro    duplicado  ");

            var result = await fixture.Handler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Status.Should().Be(FinancialMovementStatus.Cancelled);
            result.Value.CancellationReason.Should().Be("Registro duplicado");
            result.Value.ConfirmedByUserId.Should().Be(fixture.ConfirmerId);
            result.Value.ConfirmedAt.Should().Be(fixture.ConfirmedAt);
            result.Value.CancelledByUserId.Should().Be(fixture.UserId);
            result.Value.CancelledAt.Should().Be(fixture.Now);
            fixture.Movements.Verify(
                repository => repository.SetOriginalVersion(movement, 7),
                Times.Once
            );
            fixture.UnitOfWork.Verify(
                unit => unit.SaveChangesAsync(CancellationToken.None),
                Times.Once
            );
        }

        [Theory]
        [InlineData(false, true, true, ErrorCodes.BusinessAccessForbidden)]
        [InlineData(true, false, true, ErrorCodes.BusinessAccessForbidden)]
        [InlineData(true, true, false, ErrorCodes.BusinessUnavailable)]
        public async Task Handle_Should_Reject_Invalid_Access_Context(
            bool membershipActive,
            bool roleActive,
            bool businessActive,
            string expectedCode
        )
        {
            var fixture = new Fixture();
            fixture.ConfigureContext(membershipActive, roleActive, businessActive);

            var result = await fixture
                .Handler()
                .Handle(fixture.Command("Motivo válido"), CancellationToken.None);

            result.Error.Code.Should().Be(expectedCode);
            fixture.UnitOfWork.Verify(
                unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Permission_Movement_And_NonConfirmed_Status()
        {
            var forbidden = new Fixture();
            forbidden
                .Permissions.Setup(repository =>
                    repository.ListActiveCodesByRoleIdAsync(
                        forbidden.RoleId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync([]);
            (
                await forbidden
                    .Handler()
                    .Handle(forbidden.Command("Motivo válido"), CancellationToken.None)
            )
                .Error.Code.Should()
                .Be(ErrorCodes.MovementAccessForbidden);

            var missing = new Fixture();
            missing.UseMovement(null);
            (
                await missing
                    .Handler()
                    .Handle(missing.Command("Motivo válido"), CancellationToken.None)
            )
                .Error.Code.Should()
                .Be(ErrorCodes.MovementNotFound);

            var draft = new Fixture();
            draft.UseMovement(draft.DraftMovement());
            (await draft.Handler().Handle(draft.Command("Motivo válido"), CancellationToken.None))
                .Error.Code.Should()
                .Be(ErrorCodes.MovementNotEditable);
        }

        [Fact]
        public async Task Handle_Should_Map_Concurrency_Unexpected_And_Cancellation_Errors()
        {
            var concurrency = new Fixture();
            concurrency.UseMovement(concurrency.ConfirmedMovement());
            concurrency
                .UnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new ApplicationErrorException(
                        ErrorCodes.ConcurrencyConflict,
                        ErrorMessages.ConcurrencyConflict,
                        ApplicationErrorType.Conflict
                    )
                );
            (
                await concurrency
                    .Handler()
                    .Handle(concurrency.Command("Motivo válido"), CancellationToken.None)
            )
                .Error.Code.Should()
                .Be(ErrorCodes.ConcurrencyConflict);

            var failure = new Fixture();
            failure.UseMovement(failure.ConfirmedMovement());
            failure
                .UnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());
            (
                await failure
                    .Handler()
                    .Handle(failure.Command("Motivo válido"), CancellationToken.None)
            )
                .Error.Code.Should()
                .Be(ErrorCodes.MovementCancellationFailed);

            var cancelled = new Fixture();
            cancelled.UseMovement(cancelled.ConfirmedMovement());
            cancelled
                .UnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            var action = () =>
                cancelled
                    .Handler()
                    .Handle(cancelled.Command("Motivo válido"), CancellationToken.None);
            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        private sealed class Fixture
        {
            public Guid BusinessId { get; } = Guid.NewGuid();
            public Guid MovementId { get; } = Guid.NewGuid();
            public Guid UserId { get; } = Guid.NewGuid();
            public Guid ConfirmerId { get; } = Guid.NewGuid();
            public Guid RoleId { get; } = Guid.NewGuid();
            public DateTimeOffset ConfirmedAt { get; } = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);
            public DateTimeOffset Now { get; } = new(2026, 10, 11, 14, 0, 0, TimeSpan.Zero);
            public Mock<IBusinessMembershipRepository> Memberships { get; } = new();
            public Mock<IPermissionRepository> Permissions { get; } = new();
            public Mock<IFinancialMovementRepository> Movements { get; } = new();
            public Mock<IUnitOfWork> UnitOfWork { get; } = new();
            public Mock<IClock> Clock { get; } = new();

            public Fixture()
            {
                ConfigureContext(true, true, true);
                Permissions
                    .Setup(repository =>
                        repository.ListActiveCodesByRoleIdAsync(
                            RoleId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync([SystemPermissions.MovementCancel.Code]);
                UnitOfWork
                    .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(1);
                Clock.SetupGet(clock => clock.UtcNow).Returns(Now);
            }

            public void ConfigureContext(
                bool membershipActive,
                bool roleActive,
                bool businessActive
            ) =>
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
                            businessActive ? BusinessStatus.Active : BusinessStatus.Inactive,
                            Guid.NewGuid(),
                            membershipActive
                                ? BusinessMembershipStatus.Active
                                : BusinessMembershipStatus.Inactive,
                            RoleId,
                            "OWNER",
                            roleActive
                        )
                    );

            public FinancialMovement DraftMovement() =>
                FinancialMovement.CreateDraft(
                    BusinessId,
                    FinancialMovementType.Expense,
                    new DateOnly(2026, 10, 11),
                    null,
                    "PEN",
                    UserId,
                    ConfirmedAt.AddHours(-1)
                );

            public FinancialMovement ConfirmedMovement()
            {
                var movement = DraftMovement();
                movement.AddExpenseItem(
                    Guid.NewGuid(),
                    "Concepto",
                    2m,
                    5m,
                    UserId,
                    ConfirmedAt.AddMinutes(-1)
                );
                movement.Confirm(ConfirmerId, ConfirmedAt);
                return movement;
            }

            public void UseMovement(FinancialMovement movement) =>
                Movements
                    .Setup(repository =>
                        repository.GetTrackedByIdAndBusinessAsync(
                            MovementId,
                            BusinessId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(movement);

            public CancelFinancialMovementCommand Command(string reason) =>
                new()
                {
                    BusinessId = BusinessId,
                    MovementId = MovementId,
                    CurrentUserId = UserId,
                    MovementVersion = 7,
                    Reason =
                        reason.Length >= FinancialMovementConstraints.CancellationReasonMinLength
                            ? reason
                            : new string(
                                'a',
                                FinancialMovementConstraints.CancellationReasonMinLength
                            ),
                };

            public CancelFinancialMovementCommandHandler Handler() =>
                new(
                    Memberships.Object,
                    Permissions.Object,
                    Movements.Object,
                    UnitOfWork.Object,
                    Clock.Object
                );
        }
    }
}
