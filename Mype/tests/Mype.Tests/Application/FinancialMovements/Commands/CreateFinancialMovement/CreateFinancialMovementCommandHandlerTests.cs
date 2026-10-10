using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialMovements.Commands.CreateFinancialMovement;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;

namespace Mype.Tests.Application.FinancialMovements.Commands.CreateFinancialMovement
{
    public class CreateFinancialMovementCommandHandlerTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid();
        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly Guid RoleId = Guid.NewGuid();
        private static readonly DateTimeOffset UtcNow = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        private readonly Mock<IBusinessMembershipRepository> _memberships = new();
        private readonly Mock<IPermissionRepository> _permissions = new();
        private readonly Mock<IFinancialMovementRepository> _movements = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IClock> _clock = new();
        private readonly CreateFinancialMovementCommandHandler _handler;

        public CreateFinancialMovementCommandHandlerTests() =>
            _handler = new(
                _memberships.Object,
                _permissions.Object,
                _movements.Object,
                _unitOfWork.Object,
                _clock.Object
            );

        [Fact]
        public async Task Handle_Should_Create_Draft_When_Request_Is_Valid()
        {
            SetupAccess(SystemPermissions.MovementCreate.Code);
            _clock.SetupGet(x => x.UtcNow).Returns(UtcNow);
            _unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            FinancialMovement captured = null;
            _movements
                .Setup(x =>
                    x.AddAsync(It.IsAny<FinancialMovement>(), It.IsAny<CancellationToken>())
                )
                .Callback<FinancialMovement, CancellationToken>(
                    (movement, _) => captured = movement
                )
                .Returns(Task.CompletedTask);

            var result = await _handler.Handle(Command(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            captured.Should().NotBeNull();
            captured.CurrencyCode.Should().Be("PEN");
            captured.Status.Should().Be(FinancialMovementStatus.Draft);
            result.Value.Id.Should().Be(captured.Id);
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
                .BeSameAs(CreateFinancialMovementErrors.BusinessAccessForbidden);
            _permissions.Verify(
                x =>
                    x.ListActiveCodesByRoleIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Theory]
        [InlineData(BusinessMembershipStatus.Inactive, true)]
        [InlineData(BusinessMembershipStatus.Active, false)]
        public async Task Handle_Should_Reject_Inactive_Membership_Or_Role(
            BusinessMembershipStatus status,
            bool roleActive
        )
        {
            SetupContext(BusinessStatus.Active, status, roleActive);
            (await _handler.Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(CreateFinancialMovementErrors.BusinessAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Reject_Inactive_Business()
        {
            SetupContext(BusinessStatus.Inactive);
            (await _handler.Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(CreateFinancialMovementErrors.BusinessUnavailable);
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Permission()
        {
            SetupAccess();
            (await _handler.Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(CreateFinancialMovementErrors.MovementAccessForbidden);
            _movements.Verify(
                x => x.AddAsync(It.IsAny<FinancialMovement>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Return_CreationFailed_For_Unknown_Error()
        {
            SetupAccess(SystemPermissions.MovementCreate.Code);
            _clock.SetupGet(x => x.UtcNow).Returns(UtcNow);
            _unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());
            (await _handler.Handle(Command(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(CreateFinancialMovementErrors.MovementCreationFailed);
        }

        [Fact]
        public async Task Handle_Should_Propagate_Cancellation()
        {
            SetupAccess(SystemPermissions.MovementCreate.Code);
            _clock.SetupGet(x => x.UtcNow).Returns(UtcNow);
            _unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            await FluentActions
                .Invoking(() => _handler.Handle(Command(), CancellationToken.None))
                .Should()
                .ThrowAsync<OperationCanceledException>();
        }

        private void SetupContext(
            BusinessStatus businessStatus = BusinessStatus.Active,
            BusinessMembershipStatus membershipStatus = BusinessMembershipStatus.Active,
            bool roleActive = true
        ) =>
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
                        businessStatus,
                        Guid.NewGuid(),
                        membershipStatus,
                        RoleId,
                        "CUSTOM",
                        roleActive
                    )
                );

        private void SetupAccess(params string[] permissions)
        {
            SetupContext();
            _permissions
                .Setup(x => x.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permissions);
        }

        private static CreateFinancialMovementCommand Command() =>
            new()
            {
                BusinessId = BusinessId,
                CurrentUserId = UserId,
                Type = FinancialMovementType.Sale,
                MovementDate = new DateOnly(2026, 10, 10),
                Description = "Venta",
            };
    }
}
