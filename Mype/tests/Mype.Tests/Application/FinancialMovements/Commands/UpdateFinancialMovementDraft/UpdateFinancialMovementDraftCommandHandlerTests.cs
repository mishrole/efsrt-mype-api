using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialMovements.Commands.UpdateFinancialMovementDraft;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.FinancialMovements.Commands.UpdateFinancialMovementDraft
{
    public class UpdateFinancialMovementDraftCommandHandlerTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid(),
            MovementId = Guid.NewGuid();
        private static readonly DateTimeOffset UtcNow = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        private readonly Mock<IBusinessMembershipRepository> _memberships = new();
        private readonly Mock<IPermissionRepository> _permissions = new();
        private readonly Mock<IFinancialMovementRepository> _movements = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IClock> _clock = new();
        private readonly UpdateFinancialMovementDraftCommandHandler _handler;

        public UpdateFinancialMovementDraftCommandHandlerTests() =>
            _handler = new(
                _memberships.Object,
                _permissions.Object,
                _movements.Object,
                _unitOfWork.Object,
                _clock.Object
            );

        [Fact]
        public async Task Handle_Should_Update_Draft()
        {
            var movement = Movement();
            SetupAccess(SystemPermissions.MovementUpdate.Code);
            _movements
                .Setup(x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        MovementId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(movement);
            _clock.SetupGet(x => x.UtcNow).Returns(UtcNow.AddHours(1));
            _unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            var result = await _handler.Handle(Command(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            movement.Description.Should().Be("Actualizada");
            _movements.Verify(x => x.SetOriginalVersion(movement, 7), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Context()
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
            (await _handler.Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateFinancialMovementDraftErrors.BusinessAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Reject_Inactive_Business()
        {
            SetupContext(BusinessStatus.Inactive);
            (await _handler.Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateFinancialMovementDraftErrors.BusinessUnavailable);
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Permission()
        {
            SetupAccess();
            (await _handler.Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateFinancialMovementDraftErrors.MovementAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound()
        {
            SetupAccess(SystemPermissions.MovementUpdate.Code);
            _movements
                .Setup(x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        MovementId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((FinancialMovement)null);
            (await _handler.Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateFinancialMovementDraftErrors.MovementNotFound);
        }

        [Fact]
        public async Task Handle_Should_Reject_Closed_Movement()
        {
            var movement = Movement();
            typeof(FinancialMovement)
                .GetProperty(
                    nameof(FinancialMovement.Status),
                    BindingFlags.Instance | BindingFlags.Public
                )
                .SetValue(movement, FinancialMovementStatus.Confirmed);
            SetupAccess(SystemPermissions.MovementUpdate.Code);
            _movements
                .Setup(x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        MovementId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(movement);
            (await _handler.Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateFinancialMovementDraftErrors.MovementNotEditable);
        }

        [Fact]
        public async Task Handle_Should_Map_Concurrency()
        {
            SetupSuccessful();
            _unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new ApplicationErrorException(
                        ErrorCodes.MovementConcurrencyConflict,
                        "error",
                        ApplicationErrorType.Conflict
                    )
                );
            (await _handler.Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateFinancialMovementDraftErrors.MovementConcurrencyConflict);
        }

        [Fact]
        public async Task Handle_Should_Return_UpdateFailed()
        {
            SetupSuccessful();
            _unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());
            (await _handler.Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(UpdateFinancialMovementDraftErrors.MovementUpdateFailed);
        }

        [Fact]
        public async Task Handle_Should_Propagate_Cancellation()
        {
            SetupSuccessful();
            _unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            await FluentActions
                .Invoking(() => _handler.Handle(Command(), CancellationToken.None))
                .Should()
                .ThrowAsync<OperationCanceledException>();
        }

        private void SetupContext(BusinessStatus status = BusinessStatus.Active) =>
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
                        "Bodega",
                        "PEN",
                        status,
                        Guid.NewGuid(),
                        BusinessMembershipStatus.Active,
                        RoleId,
                        "CUSTOM",
                        true
                    )
                );

        private void SetupAccess(params string[] permissions)
        {
            SetupContext();
            _permissions
                .Setup(x => x.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permissions);
        }

        private void SetupSuccessful()
        {
            SetupAccess(SystemPermissions.MovementUpdate.Code);
            _movements
                .Setup(x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        MovementId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(Movement());
            _clock.SetupGet(x => x.UtcNow).Returns(UtcNow.AddHours(1));
        }

        private static FinancialMovement Movement()
        {
            var movement = FinancialMovement.CreateDraft(
                BusinessId,
                FinancialMovementType.Sale,
                new DateOnly(2026, 10, 10),
                "Inicial",
                "PEN",
                UserId,
                UtcNow
            );
            typeof(FinancialMovement)
                .GetProperty(
                    nameof(FinancialMovement.Id),
                    BindingFlags.Instance | BindingFlags.Public
                )
                .SetValue(movement, MovementId);
            return movement;
        }

        private static UpdateFinancialMovementDraftCommand Command() =>
            new()
            {
                BusinessId = BusinessId,
                MovementId = MovementId,
                CurrentUserId = UserId,
                MovementDate = new DateOnly(2026, 10, 11),
                Description = "Actualizada",
                Version = 7,
            };
    }
}
