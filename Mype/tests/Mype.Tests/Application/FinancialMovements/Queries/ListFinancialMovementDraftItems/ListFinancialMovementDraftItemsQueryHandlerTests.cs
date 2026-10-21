using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.FinancialMovements.Models;
using Mype.Application.FinancialMovements.Queries.ListFinancialMovementDraftItems;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;

namespace Mype.Tests.Application.FinancialMovements.Queries.ListFinancialMovementDraftItems
{
    public sealed class ListFinancialMovementDraftItemsQueryHandlerTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid();
        private static readonly Guid MovementId = Guid.NewGuid();
        private static readonly Guid CurrentUserId = Guid.NewGuid();
        private static readonly Guid RoleId = Guid.NewGuid();
        private readonly Mock<IBusinessMembershipRepository> _membershipRepositoryMock = new();
        private readonly Mock<IPermissionRepository> _permissionRepositoryMock = new();
        private readonly Mock<IFinancialMovementRepository> _financialMovementRepositoryMock =
            new();

        [Fact]
        public async Task Handle_Should_Map_Sale_Item_Specific_Information()
        {
            SetupAuthorizedContext();
            var projection = CreateProjection(FinancialMovementType.Sale, 2m, 5m, 2m);
            _financialMovementRepositoryMock
                .Setup(repository =>
                    repository.GetDraftItemsByIdAndBusinessAsync(
                        MovementId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(projection);

            var result = await CreateHandler().Handle(CreateQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.MovementVersion.Should().Be(7);
            var item = result.Value.Items.Should().ContainSingle().Subject;
            item.ProductId.Should().NotBeNull();
            item.ProductCategoryId.Should().NotBeNull();
            item.ProductName.Should().Be("Gaseosa");
            item.Description.Should().BeNull();
            item.EstimatedCost.Should().Be(4m);
            item.EstimatedMargin.Should().Be(6m);
            item.Version.Should().Be(3);
        }

        [Fact]
        public async Task Handle_Should_Map_Expense_Item_Specific_Information()
        {
            SetupAuthorizedContext();
            var projection = CreateProjection(FinancialMovementType.Expense, 2m, 5m, null);
            _financialMovementRepositoryMock
                .Setup(repository =>
                    repository.GetDraftItemsByIdAndBusinessAsync(
                        MovementId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(projection);

            var result = await CreateHandler().Handle(CreateQuery(), CancellationToken.None);

            var item = result.Value.Items.Should().ContainSingle().Subject;
            item.ProductId.Should().BeNull();
            item.ProductCategoryId.Should().BeNull();
            item.ProductName.Should().BeNull();
            item.Description.Should().Be("Movilidad");
            item.UnitCostSnapshot.Should().BeNull();
            item.EstimatedCost.Should().BeNull();
            item.EstimatedMargin.Should().BeNull();
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public async Task Handle_Should_Reject_Inactive_Membership_Or_Role(
            bool membershipIsActive,
            bool roleIsActive
        )
        {
            SetupContext(
                membershipIsActive
                    ? BusinessMembershipStatus.Active
                    : BusinessMembershipStatus.Inactive,
                roleIsActive,
                BusinessStatus.Active
            );

            var result = await CreateHandler().Handle(CreateQuery(), CancellationToken.None);

            result
                .Error.Should()
                .BeSameAs(ListFinancialMovementDraftItemsErrors.BusinessAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Business_Context()
        {
            _membershipRepositoryMock
                .Setup(repository =>
                    repository.GetContextByBusinessAndUserAsync(
                        BusinessId,
                        CurrentUserId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((BusinessContextProjection)null);

            var result = await CreateHandler().Handle(CreateQuery(), CancellationToken.None);

            result
                .Error.Should()
                .BeSameAs(ListFinancialMovementDraftItemsErrors.BusinessAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Reject_Inactive_Business()
        {
            SetupContext(BusinessMembershipStatus.Active, true, BusinessStatus.Inactive);

            var result = await CreateHandler().Handle(CreateQuery(), CancellationToken.None);

            result
                .Error.Should()
                .BeSameAs(ListFinancialMovementDraftItemsErrors.BusinessUnavailable);
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Read_Permission()
        {
            SetupContext(BusinessMembershipStatus.Active, true, BusinessStatus.Active);
            _permissionRepositoryMock
                .Setup(repository =>
                    repository.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(Array.Empty<string>());

            var result = await CreateHandler().Handle(CreateQuery(), CancellationToken.None);

            result
                .Error.Should()
                .BeSameAs(ListFinancialMovementDraftItemsErrors.MovementAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Draft_Does_Not_Exist()
        {
            SetupAuthorizedContext();
            _financialMovementRepositoryMock
                .Setup(repository =>
                    repository.GetDraftItemsByIdAndBusinessAsync(
                        MovementId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((FinancialMovementDraftItemsProjection)null);

            var result = await CreateHandler().Handle(CreateQuery(), CancellationToken.None);

            result.Error.Should().BeSameAs(ListFinancialMovementDraftItemsErrors.MovementNotFound);
        }

        [Fact]
        public async Task Handle_Should_Return_Empty_Collection_When_Draft_Has_No_Active_Items()
        {
            SetupAuthorizedContext();
            var projection = new FinancialMovementDraftItemsProjection(
                MovementId,
                FinancialMovementType.Sale,
                1,
                Array.Empty<FinancialMovementDraftItemProjection>()
            );
            _financialMovementRepositoryMock
                .Setup(repository =>
                    repository.GetDraftItemsByIdAndBusinessAsync(
                        MovementId,
                        BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(projection);

            var result = await CreateHandler().Handle(CreateQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().BeEmpty();
        }

        private void SetupAuthorizedContext()
        {
            SetupContext(BusinessMembershipStatus.Active, true, BusinessStatus.Active);
            _permissionRepositoryMock
                .Setup(repository =>
                    repository.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(new[] { SystemPermissions.MovementRead.Code });
        }

        private void SetupContext(
            BusinessMembershipStatus membershipStatus,
            bool roleIsActive,
            BusinessStatus businessStatus
        )
        {
            _membershipRepositoryMock
                .Setup(repository =>
                    repository.GetContextByBusinessAndUserAsync(
                        BusinessId,
                        CurrentUserId,
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
                        roleIsActive
                    )
                );
        }

        private ListFinancialMovementDraftItemsQueryHandler CreateHandler()
        {
            return new ListFinancialMovementDraftItemsQueryHandler(
                _membershipRepositoryMock.Object,
                _permissionRepositoryMock.Object,
                _financialMovementRepositoryMock.Object
            );
        }

        private static ListFinancialMovementDraftItemsQuery CreateQuery()
        {
            return new ListFinancialMovementDraftItemsQuery
            {
                BusinessId = BusinessId,
                MovementId = MovementId,
                CurrentUserId = CurrentUserId,
            };
        }

        private static FinancialMovementDraftItemsProjection CreateProjection(
            FinancialMovementType movementType,
            decimal quantity,
            decimal unitAmount,
            decimal? unitCostSnapshot
        )
        {
            var isSale = movementType == FinancialMovementType.Sale;
            return new FinancialMovementDraftItemsProjection(
                MovementId,
                movementType,
                7,
                new[]
                {
                    new FinancialMovementDraftItemProjection(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        isSale ? "Ventas" : "Movilidad",
                        isSale ? Guid.NewGuid() : null,
                        isSale ? Guid.NewGuid() : null,
                        isSale ? "Gaseosa" : "Movilidad",
                        quantity,
                        unitAmount,
                        unitCostSnapshot,
                        quantity * unitAmount,
                        3
                    ),
                }
            );
        }
    }
}
