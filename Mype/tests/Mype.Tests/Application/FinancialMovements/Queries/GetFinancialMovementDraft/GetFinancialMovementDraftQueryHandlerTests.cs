using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.FinancialMovements.Models;
using Mype.Application.FinancialMovements.Queries.GetFinancialMovementDraft;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;

namespace Mype.Tests.Application.FinancialMovements.Queries.GetFinancialMovementDraft
{
    public class GetFinancialMovementDraftQueryHandlerTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid(),
            MovementId = Guid.NewGuid();
        private readonly Mock<IBusinessMembershipRepository> _memberships = new();
        private readonly Mock<IPermissionRepository> _permissions = new();
        private readonly Mock<IFinancialMovementRepository> _movements = new();
        private readonly GetFinancialMovementDraftQueryHandler _handler;

        public GetFinancialMovementDraftQueryHandlerTests() =>
            _handler = new(_memberships.Object, _permissions.Object, _movements.Object);

        [Fact]
        public async Task Handle_Should_Return_Draft()
        {
            SetupAccess(SystemPermissions.MovementRead.Code);
            _movements
                .Setup(x =>
                    x.GetDraftByIdAndBusinessAsync(
                        MovementId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    new FinancialMovementDraftProjection(
                        MovementId,
                        BusinessId,
                        FinancialMovementType.Sale,
                        FinancialMovementStatus.Draft,
                        new DateOnly(2026, 10, 10),
                        "Venta",
                        "PEN",
                        0m,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow,
                        1
                    )
                );
            (await _handler.Handle(Query(), CancellationToken.None)).IsSuccess.Should().BeTrue();
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
            (await _handler.Handle(Query(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(GetFinancialMovementDraftErrors.BusinessAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Reject_Inactive_Business()
        {
            SetupContext(BusinessStatus.Inactive);
            (await _handler.Handle(Query(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(GetFinancialMovementDraftErrors.BusinessUnavailable);
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Permission()
        {
            SetupAccess();
            (await _handler.Handle(Query(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(GetFinancialMovementDraftErrors.MovementAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound()
        {
            SetupAccess(SystemPermissions.MovementRead.Code);
            _movements
                .Setup(x =>
                    x.GetDraftByIdAndBusinessAsync(
                        MovementId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((FinancialMovementDraftProjection)null);
            (await _handler.Handle(Query(), CancellationToken.None))
                .Error.Should()
                .BeSameAs(GetFinancialMovementDraftErrors.MovementNotFound);
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

        private static GetFinancialMovementDraftQuery Query() =>
            new()
            {
                BusinessId = BusinessId,
                MovementId = MovementId,
                CurrentUserId = UserId,
            };
    }
}
