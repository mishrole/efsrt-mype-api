using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Categories.Interfaces;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.FinancialMovements.Models;
using Mype.Application.FinancialMovements.Queries.ListFinancialMovementHistory;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.FinancialMovements.Queries.ListFinancialMovementHistory
{
    public sealed class ListFinancialMovementHistoryQueryHandlerTests
    {
        [Fact]
        public async Task Handle_Should_Return_Page_And_Forward_All_Filters()
        {
            var fixture = new Fixture(BusinessStatus.Inactive);
            var categoryId = Guid.NewGuid();
            var movementId = Guid.NewGuid();
            fixture
                .Categories.Setup(repository =>
                    repository.ExistsByIdAndBusinessAsync(
                        categoryId,
                        fixture.BusinessId,
                        It.IsAny<System.Threading.CancellationToken>()
                    )
                )
                .ReturnsAsync(true);
            fixture
                .Movements.Setup(repository =>
                    repository.SearchHistoryByBusinessAsync(
                        fixture.BusinessId,
                        It.IsAny<FinancialMovementHistoryFilters>(),
                        It.IsAny<System.Threading.CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    new FinancialMovementHistoryPageProjection(
                        [
                            new(
                                movementId,
                                FinancialMovementType.Sale,
                                FinancialMovementStatus.Confirmed,
                                new DateOnly(2026, 10, 10),
                                "Venta",
                                "PEN",
                                10m,
                                2,
                                true,
                                fixture.Now,
                                fixture.Now,
                                null,
                                null
                            ),
                        ],
                        2,
                        5,
                        8
                    )
                );
            var query = fixture.Query();
            query.Type = FinancialMovementType.Sale;
            query.Status = FinancialMovementStatus.Confirmed;
            query.DateFrom = new DateOnly(2026, 10, 1);
            query.DateTo = new DateOnly(2026, 10, 31);
            query.CategoryId = categoryId;
            query.Page = 2;
            query.PageSize = 5;

            var result = await fixture.Handler().Handle(query, default);

            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().ContainSingle(item => item.Id == movementId);
            result.Value.HasPreviousPage.Should().BeTrue();
            result.Value.HasNextPage.Should().BeFalse();
            fixture.Movements.Verify(
                repository =>
                    repository.SearchHistoryByBusinessAsync(
                        fixture.BusinessId,
                        It.Is<FinancialMovementHistoryFilters>(filters =>
                            filters.Type == query.Type
                            && filters.Status == query.Status
                            && filters.DateFrom == query.DateFrom
                            && filters.DateTo == query.DateTo
                            && filters.CategoryId == categoryId
                            && filters.Page == 2
                            && filters.PageSize == 5
                        ),
                        It.IsAny<System.Threading.CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Allow_Inactive_Business_With_Active_Membership_And_HistoryRead()
        {
            var fixture = new Fixture(BusinessStatus.Inactive);
            fixture
                .Movements.Setup(repository =>
                    repository.SearchHistoryByBusinessAsync(
                        fixture.BusinessId,
                        It.IsAny<FinancialMovementHistoryFilters>(),
                        It.IsAny<System.Threading.CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    new FinancialMovementHistoryPageProjection(
                        Array.Empty<FinancialMovementHistoryItemProjection>(),
                        1,
                        20,
                        0
                    )
                );

            var result = await fixture.Handler().Handle(fixture.Query(), default);

            result.IsSuccess.Should().BeTrue();
            result.Value.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_Reject_Category_From_Another_Business()
        {
            var fixture = new Fixture(BusinessStatus.Active);
            fixture
                .Categories.Setup(repository =>
                    repository.ExistsByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        fixture.BusinessId,
                        It.IsAny<System.Threading.CancellationToken>()
                    )
                )
                .ReturnsAsync(false);

            var query = fixture.Query();
            query.CategoryId = Guid.NewGuid();

            var result = await fixture.Handler().Handle(query, default);

            result.Error.Code.Should().Be(ErrorCodes.CategoryNotFound);
            fixture.Movements.Verify(
                repository =>
                    repository.SearchHistoryByBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<FinancialMovementHistoryFilters>(),
                        It.IsAny<System.Threading.CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Reject_Inactive_Membership_Or_Missing_Permission()
        {
            var inactive = new Fixture(BusinessStatus.Active, BusinessMembershipStatus.Inactive);
            (await inactive.Handler().Handle(inactive.Query(), default))
                .Error.Code.Should()
                .Be(ErrorCodes.BusinessAccessForbidden);

            var forbidden = new Fixture(BusinessStatus.Active, BusinessMembershipStatus.Active, []);
            (await forbidden.Handler().Handle(forbidden.Query(), default))
                .Error.Code.Should()
                .Be(ErrorCodes.MovementAccessForbidden);
        }

        private sealed class Fixture
        {
            public Guid BusinessId { get; } = Guid.NewGuid();
            public Guid UserId { get; } = Guid.NewGuid();
            public Guid RoleId { get; } = Guid.NewGuid();
            public DateTimeOffset Now { get; } = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);
            public Mock<IBusinessMembershipRepository> Memberships { get; } = new();
            public Mock<IPermissionRepository> Permissions { get; } = new();
            public Mock<ICategoryRepository> Categories { get; } = new();
            public Mock<IFinancialMovementRepository> Movements { get; } = new();

            public Fixture(
                BusinessStatus status,
                BusinessMembershipStatus membership = BusinessMembershipStatus.Active,
                string[] permissions = null
            )
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
                            status,
                            Guid.NewGuid(),
                            membership,
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

            public ListFinancialMovementHistoryQuery Query() =>
                new()
                {
                    BusinessId = BusinessId,
                    CurrentUserId = UserId,
                    Page = 1,
                    PageSize = 20,
                };

            public ListFinancialMovementHistoryQueryHandler Handler() =>
                new(Memberships.Object, Permissions.Object, Categories.Object, Movements.Object);
        }
    }
}
