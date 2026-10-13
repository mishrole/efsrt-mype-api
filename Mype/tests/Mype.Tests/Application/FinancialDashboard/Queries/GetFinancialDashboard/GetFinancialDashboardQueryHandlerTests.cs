using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialDashboard.Enums;
using Mype.Application.FinancialDashboard.Interfaces;
using Mype.Application.FinancialDashboard.Models;
using Mype.Application.FinancialDashboard.Queries.GetFinancialDashboard;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.FinancialDashboard.Queries.GetFinancialDashboard
{
    public sealed class GetFinancialDashboardQueryHandlerTests
    {
        [Fact]
        public async Task Handle_Should_Calculate_CashFlow_Cost_Margin_Counts_And_Categories()
        {
            var fixture = new Fixture();
            fixture.UseProjection(
                1000m,
                new DateOnly(2026, 1, 1),
                [
                    Movement(new DateOnly(2026, 9, 30), FinancialMovementType.Sale, 100m, 40m),
                    Movement(new DateOnly(2026, 10, 2), FinancialMovementType.Sale, 500m, 280m),
                    Movement(new DateOnly(2026, 10, 3), FinancialMovementType.Expense, 150m, 0m),
                    Movement(new DateOnly(2026, 10, 4), FinancialMovementType.Sale, 300m, 120m),
                ],
                [
                    Category(
                        "Productos",
                        true,
                        FinancialMovementType.Sale,
                        new DateOnly(2026, 10, 2),
                        500m
                    ),
                    Category(
                        "Servicios",
                        false,
                        FinancialMovementType.Sale,
                        new DateOnly(2026, 10, 4),
                        300m
                    ),
                    Category(
                        "Alquiler",
                        true,
                        FinancialMovementType.Expense,
                        new DateOnly(2026, 10, 3),
                        150m
                    ),
                ]
            );

            var result = await fixture.Handler().Handle(fixture.Query(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.OpeningBalance.Should().Be(1100m);
            result.Value.CashInflow.Should().Be(800m);
            result.Value.CashOutflow.Should().Be(150m);
            result.Value.NetCashFlow.Should().Be(650m);
            result.Value.ClosingBalance.Should().Be(1750m);
            result.Value.EstimatedCost.Should().Be(400m);
            result.Value.EstimatedGrossMargin.Should().Be(400m);
            result.Value.ConfirmedSaleCount.Should().Be(2);
            result.Value.ConfirmedExpenseCount.Should().Be(1);
            result.Value.AverageSaleAmount.Should().Be(400m);
            result
                .Value.SaleByCategory.Select(item => item.CategoryName)
                .Should()
                .Equal("Productos", "Servicios");
            result.Value.SaleByCategory.Sum(item => item.Percentage).Should().Be(100m);
            result
                .Value.SaleByCategory.Single(item => item.CategoryName == "Servicios")
                .CategoryIsActive.Should()
                .BeFalse();
            result
                .Value.ExpenseByCategory.Should()
                .ContainSingle(item => item.Amount == 150m && item.Percentage == 100m);
        }

        [Fact]
        public async Task Handle_Should_Preserve_OpeningBalance_In_Empty_Period()
        {
            var fixture = new Fixture();
            fixture.UseProjection(-500m, new DateOnly(2026, 1, 1), [], []);
            var result = await fixture.Handler().Handle(fixture.Query(), CancellationToken.None);
            result.Value.OpeningBalance.Should().Be(-500m);
            result.Value.CashInflow.Should().Be(0m);
            result.Value.CashOutflow.Should().Be(0m);
            result.Value.NetCashFlow.Should().Be(0m);
            result.Value.ClosingBalance.Should().Be(-500m);
            result.Value.AverageSaleAmount.Should().Be(0m);
            result
                .Value.Trend.Should()
                .OnlyContain(point =>
                    point.ClosingBalance == -500m
                    && point.CashInflow == 0m
                    && point.CashOutflow == 0m
                );
        }

        [Theory]
        [InlineData(31, FinancialDashboardGranularity.Day)]
        [InlineData(32, FinancialDashboardGranularity.Week)]
        [InlineData(181, FinancialDashboardGranularity.Month)]
        [InlineData(1096, FinancialDashboardGranularity.Year)]
        public async Task Handle_Should_Resolve_Auto_Granularity(
            int inclusiveDays,
            FinancialDashboardGranularity expected
        )
        {
            var fixture = new Fixture();
            fixture.UseProjection(0m, fixture.Today.AddYears(-10), [], []);
            var query = fixture.Query();
            query.DateFrom = fixture.Today.AddDays(-(inclusiveDays - 1));
            query.DateTo = fixture.Today;
            query.Granularity = FinancialDashboardGranularity.Auto;
            var result = await fixture.Handler().Handle(query, CancellationToken.None);
            result.Value.Granularity.Should().Be(expected);
        }

        [Theory]
        [InlineData(FinancialDashboardGranularity.Day, 10)]
        [InlineData(FinancialDashboardGranularity.Week, 2)]
        [InlineData(FinancialDashboardGranularity.Month, 1)]
        [InlineData(FinancialDashboardGranularity.Year, 1)]
        public async Task Handle_Should_Build_Continuous_Trend(
            FinancialDashboardGranularity granularity,
            int expectedPoints
        )
        {
            var fixture = new Fixture();
            fixture.UseProjection(
                100m,
                new DateOnly(2026, 1, 1),
                [Movement(new DateOnly(2026, 10, 3), FinancialMovementType.Sale, 50m, 10m)],
                []
            );
            var query = fixture.Query();
            query.DateFrom = new DateOnly(2026, 10, 1);
            query.DateTo = new DateOnly(2026, 10, 10);
            query.Granularity = granularity;
            var result = await fixture.Handler().Handle(query, CancellationToken.None);
            result.Value.Trend.Should().HaveCount(expectedPoints);
            result.Value.Trend.First().DateFrom.Should().Be(query.DateFrom);
            result.Value.Trend.Last().DateTo.Should().Be(query.DateTo);
            result.Value.Trend.Last().ClosingBalance.Should().Be(150m);
        }

        [Fact]
        public async Task Handle_Should_Apply_Default_Thirty_Day_Period()
        {
            var fixture = new Fixture();
            fixture.UseProjection(0m, new DateOnly(2026, 1, 1), [], []);
            var query = fixture.Query();
            query.DateFrom = null;
            query.DateTo = null;
            var result = await fixture.Handler().Handle(query, CancellationToken.None);
            result.Value.DateTo.Should().Be(fixture.Today);
            result.Value.DateFrom.Should().Be(fixture.Today.AddDays(-29));
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
            fixture.ConfigureContext(BusinessStatus.Active, membershipActive, roleActive);
            var result = await fixture.Handler().Handle(fixture.Query(), CancellationToken.None);
            result.Error.Code.Should().Be(ErrorCodes.BusinessAccessForbidden);
            fixture.Dashboard.Verify(
                repository =>
                    repository.GetAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<DateOnly>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Allow_Inactive_Business_For_Historical_Read()
        {
            var fixture = new Fixture();
            fixture.ConfigureContext(BusinessStatus.Inactive, true, true);
            fixture.UseProjection(0m, new DateOnly(2026, 1, 1), [], []);
            var result = await fixture.Handler().Handle(fixture.Query(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Dashboard_Permission()
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
            var result = await fixture.Handler().Handle(fixture.Query(), CancellationToken.None);
            result.Error.Code.Should().Be(ErrorCodes.DashboardAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Return_OpeningBalanceNotFound()
        {
            var fixture = new Fixture();
            fixture.UseNullProjection();
            var result = await fixture.Handler().Handle(fixture.Query(), CancellationToken.None);
            result.Error.Code.Should().Be(ErrorCodes.OpeningBalanceNotFound);
        }

        [Fact]
        public async Task Handle_Should_Map_Unexpected_Error_And_Rethrow_Cancellation()
        {
            var fixture = new Fixture();

            fixture
                .Dashboard.Setup(repository =>
                    repository.GetAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<DateOnly>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ThrowsAsync(new InvalidOperationException());

            var failed = await fixture.Handler().Handle(fixture.Query(), CancellationToken.None);

            failed.Error.Code.Should().Be(ErrorCodes.FinancialDashboardQueryFailed);

            fixture
                .Dashboard.Setup(repository =>
                    repository.GetAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<DateOnly>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ThrowsAsync(new OperationCanceledException());

            var action = () => fixture.Handler().Handle(fixture.Query(), CancellationToken.None);

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        private static FinancialDashboardMovementProjection Movement(
            DateOnly date,
            FinancialMovementType type,
            decimal amount,
            decimal cost
        ) => new(date, (int)type, amount, cost);

        private static FinancialDashboardCategoryProjection Category(
            string name,
            bool active,
            FinancialMovementType type,
            DateOnly date,
            decimal amount
        ) => new(Guid.NewGuid(), name, active, (int)type, date, amount);

        private sealed class Fixture
        {
            public Guid BusinessId { get; } = Guid.NewGuid();
            public Guid UserId { get; } = Guid.NewGuid();
            public Guid RoleId { get; } = Guid.NewGuid();
            public DateOnly Today { get; } = new(2026, 10, 13);
            public Mock<IBusinessMembershipRepository> Memberships { get; } = new();
            public Mock<IPermissionRepository> Permissions { get; } = new();
            public Mock<IFinancialDashboardRepository> Dashboard { get; } = new();
            public Mock<IClock> Clock { get; } = new();

            public Fixture()
            {
                ConfigureContext(BusinessStatus.Active, true, true);
                Permissions
                    .Setup(r =>
                        r.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>())
                    )
                    .ReturnsAsync([SystemPermissions.DashboardRead.Code]);
                Clock
                    .SetupGet(c => c.UtcNow)
                    .Returns(
                        new DateTimeOffset(Today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
                    );
            }

            public void ConfigureContext(BusinessStatus status, bool member, bool role) =>
                Memberships
                    .Setup(r =>
                        r.GetContextByBusinessAndUserAsync(
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
                            member
                                ? BusinessMembershipStatus.Active
                                : BusinessMembershipStatus.Inactive,
                            RoleId,
                            "OWNER",
                            role
                        )
                    );

            public void UseProjection(
                decimal amount,
                DateOnly date,
                FinancialDashboardMovementProjection[] movements,
                FinancialDashboardCategoryProjection[] categories
            )
            {
                Dashboard
                    .Setup(repository =>
                        repository.GetAsync(
                            BusinessId,
                            It.IsAny<DateOnly>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(
                        new FinancialDashboardProjection(amount, date, movements, categories)
                    );
            }

            public void UseNullProjection()
            {
                Dashboard
                    .Setup(repository =>
                        repository.GetAsync(
                            BusinessId,
                            It.IsAny<DateOnly>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync((FinancialDashboardProjection)null);
            }

            public GetFinancialDashboardQuery Query() =>
                new()
                {
                    BusinessId = BusinessId,
                    CurrentUserId = UserId,
                    DateFrom = new DateOnly(2026, 10, 1),
                    DateTo = new DateOnly(2026, 10, 31),
                    Granularity = FinancialDashboardGranularity.Day,
                };

            public GetFinancialDashboardQueryHandler Handler() =>
                new(Memberships.Object, Permissions.Object, Dashboard.Object, Clock.Object);
        }
    }
}
