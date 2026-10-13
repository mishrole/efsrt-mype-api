using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.Businesses.Commands.ReplaceOpeningBalance;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.Businesses.Commands.ReplaceOpeningBalance
{
    public sealed class ReplaceOpeningBalanceCommandHandlerTests
    {
        [Fact]
        public async Task Handle_Should_Replace_Current_OpeningBalance_And_Save_Once()
        {
            var fixture = new Fixture();
            var current = fixture.CurrentOpeningBalance();
            fixture.UseCurrent(current);

            var result = await fixture.Handler().Handle(fixture.Command(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Amount.Should().Be(-250m);
            result.Value.MovementDate.Should().Be(fixture.OpeningDate);
            current.IsCurrentOpeningBalance.Should().BeFalse();
            fixture.Movements.Verify(
                repository => repository.SetOriginalVersion(current, 7),
                Times.Once
            );
            fixture.Movements.Verify(
                repository =>
                    repository.AddAsync(
                        It.Is<FinancialMovement>(movement =>
                            movement.Type == FinancialMovementType.OpeningBalance
                            && movement.IsCurrentOpeningBalance
                            && movement.ReplacesMovementId == current.Id
                            && movement.TotalAmount == -250m
                            && movement.MovementDate == fixture.OpeningDate
                            && movement.ReplacementReason == "Conciliación inicial"
                        ),
                        CancellationToken.None
                    ),
                Times.Once
            );
            fixture.UnitOfWork.Verify(
                unit => unit.SaveChangesAsync(CancellationToken.None),
                Times.Once
            );
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public async Task Handle_Should_Reject_Inactive_Membership_Or_Role(
            bool membershipActive,
            bool roleActive
        )
        {
            var fixture = new Fixture();
            fixture.ConfigureContext(membershipActive, roleActive);

            var result = await fixture.Handler().Handle(fixture.Command(), CancellationToken.None);

            result.Error.Code.Should().Be(ErrorCodes.BusinessAccessForbidden);
            fixture.Movements.Verify(
                repository =>
                    repository.GetCurrentOpeningBalanceTrackedAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Replace_Permission()
        {
            var fixture = new Fixture();
            fixture
                .Permissions.Setup(repository =>
                    repository.ListActiveCodesByRoleIdAsync(
                        fixture.RoleId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync([]);

            var result = await fixture.Handler().Handle(fixture.Command(), CancellationToken.None);

            result.Error.Code.Should().Be(ErrorCodes.MovementAccessForbidden);
            fixture.UnitOfWork.Verify(
                unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Current_OpeningBalance_Does_Not_Exist()
        {
            var fixture = new Fixture();
            fixture.UseCurrent(null);

            var result = await fixture.Handler().Handle(fixture.Command(), CancellationToken.None);

            result.Error.Code.Should().Be(ErrorCodes.MovementNotFound);
            fixture.UnitOfWork.Verify(
                unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Map_Concurrency_Conflict()
        {
            var fixture = new Fixture();
            fixture.UseCurrent(fixture.CurrentOpeningBalance());
            fixture
                .UnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new ApplicationErrorException(
                        ErrorCodes.ConcurrencyConflict,
                        ErrorMessages.ConcurrencyConflict,
                        ApplicationErrorType.Conflict
                    )
                );

            var result = await fixture.Handler().Handle(fixture.Command(), CancellationToken.None);

            result.Error.Code.Should().Be(ErrorCodes.ConcurrencyConflict);
        }

        [Fact]
        public async Task Handle_Should_Map_Unexpected_Error()
        {
            var fixture = new Fixture();
            fixture.UseCurrent(fixture.CurrentOpeningBalance());
            fixture
                .UnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());

            var result = await fixture.Handler().Handle(fixture.Command(), CancellationToken.None);

            result.Error.Code.Should().Be(ErrorCodes.BusinessCreationFailed);
        }

        [Fact]
        public async Task Handle_Should_Rethrow_OperationCanceledException()
        {
            var fixture = new Fixture();
            fixture.UseCurrent(fixture.CurrentOpeningBalance());
            fixture
                .UnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var action = () => fixture.Handler().Handle(fixture.Command(), CancellationToken.None);

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        private sealed class Fixture
        {
            public Guid BusinessId { get; } = Guid.NewGuid();
            public Guid UserId { get; } = Guid.NewGuid();
            public Guid RoleId { get; } = Guid.NewGuid();
            public DateOnly OpeningDate { get; } = new(2026, 10, 1);
            public DateTimeOffset UtcNow { get; } = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
            public Mock<IBusinessMembershipRepository> Memberships { get; } = new();
            public Mock<IPermissionRepository> Permissions { get; } = new();
            public Mock<IFinancialMovementRepository> Movements { get; } = new();
            public Mock<IUnitOfWork> UnitOfWork { get; } = new();
            public Mock<IClock> Clock { get; } = new();

            public Fixture()
            {
                ConfigureContext(true, true);
                Permissions
                    .Setup(repository =>
                        repository.ListActiveCodesByRoleIdAsync(
                            RoleId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync([SystemPermissions.OpeningBalanceReplace.Code]);
                UnitOfWork
                    .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(2);
                Clock.SetupGet(clock => clock.UtcNow).Returns(UtcNow);
            }

            public void ConfigureContext(bool membershipActive, bool roleActive) =>
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
                            "Bodega",
                            "PEN",
                            BusinessStatus.Active,
                            Guid.NewGuid(),
                            membershipActive
                                ? BusinessMembershipStatus.Active
                                : BusinessMembershipStatus.Inactive,
                            RoleId,
                            "OWNER",
                            roleActive
                        )
                    );

            public FinancialMovement CurrentOpeningBalance() =>
                FinancialMovement.CreateOpeningBalance(
                    BusinessId,
                    OpeningDate,
                    1000m,
                    "PEN",
                    UserId,
                    UtcNow.AddDays(-1)
                );

            public void UseCurrent(FinancialMovement movement) =>
                Movements
                    .Setup(repository =>
                        repository.GetCurrentOpeningBalanceTrackedAsync(
                            BusinessId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(movement);

            public ReplaceOpeningBalanceCommand Command() =>
                new()
                {
                    BusinessId = BusinessId,
                    CurrentUserId = UserId,
                    Amount = -250m,
                    Reason = "  Conciliación inicial  ",
                    MovementVersion = 7,
                };

            public ReplaceOpeningBalanceCommandHandler Handler() =>
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
