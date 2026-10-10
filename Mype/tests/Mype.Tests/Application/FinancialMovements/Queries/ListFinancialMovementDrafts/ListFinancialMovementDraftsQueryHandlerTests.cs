using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.FinancialMovements.Models;
using Mype.Application.FinancialMovements.Queries.ListFinancialMovementDrafts;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Tests.Application.FinancialMovements.Queries.ListFinancialMovementDrafts
{
    public class ListFinancialMovementDraftsQueryHandlerTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid(), UserId = Guid.NewGuid(), RoleId = Guid.NewGuid();
        private readonly Mock<IBusinessMembershipRepository> _memberships = new(); private readonly Mock<IPermissionRepository> _permissions = new(); private readonly Mock<IFinancialMovementRepository> _movements = new(); private readonly ListFinancialMovementDraftsQueryHandler _handler;
        public ListFinancialMovementDraftsQueryHandlerTests() => _handler = new(_memberships.Object, _permissions.Object, _movements.Object);
        [Fact] public async Task Handle_Should_Return_Drafts() { SetupAccess(SystemPermissions.MovementRead.Code); _movements.Setup(x => x.ListDraftsByBusinessAsync(BusinessId, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { new FinancialMovementDraftListItemProjection(Guid.NewGuid(), FinancialMovementType.Sale, new DateOnly(2026, 10, 10), "Venta", "PEN", 0m, DateTimeOffset.UtcNow, 1) }); var result = await _handler.Handle(Query(), CancellationToken.None); result.IsSuccess.Should().BeTrue(); result.Value.Should().HaveCount(1); }
        [Fact] public async Task Handle_Should_Return_Empty_Collection() { SetupAccess(SystemPermissions.MovementRead.Code); _movements.Setup(x => x.ListDraftsByBusinessAsync(BusinessId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<FinancialMovementDraftListItemProjection>()); (await _handler.Handle(Query(), CancellationToken.None)).Value.Should().BeEmpty(); }
        [Fact] public async Task Handle_Should_Reject_Missing_Context() { _memberships.Setup(x => x.GetContextByBusinessAndUserAsync(BusinessId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync((BusinessContextProjection)null); (await _handler.Handle(Query(), CancellationToken.None)).Error.Should().BeSameAs(ListFinancialMovementDraftsErrors.BusinessAccessForbidden); }
        [Fact] public async Task Handle_Should_Reject_Inactive_Business() { SetupContext(BusinessStatus.Inactive); (await _handler.Handle(Query(), CancellationToken.None)).Error.Should().BeSameAs(ListFinancialMovementDraftsErrors.BusinessUnavailable); }
        [Fact] public async Task Handle_Should_Reject_Missing_Permission() { SetupAccess(); (await _handler.Handle(Query(), CancellationToken.None)).Error.Should().BeSameAs(ListFinancialMovementDraftsErrors.MovementAccessForbidden); }
        private void SetupContext(BusinessStatus status = BusinessStatus.Active) => _memberships.Setup(x => x.GetContextByBusinessAndUserAsync(BusinessId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(new BusinessContextProjection(BusinessId, "Bodega", "PEN", status, Guid.NewGuid(), BusinessMembershipStatus.Active, RoleId, "CUSTOM", true));
        private void SetupAccess(params string[] permissions) { SetupContext(); _permissions.Setup(x => x.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>())).ReturnsAsync(permissions); }
        private static ListFinancialMovementDraftsQuery Query() => new() { BusinessId = BusinessId, CurrentUserId = UserId };
    }
}
