using System;
using System.Threading;
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
        public async Task Handle_Should_Allow_Inactive_Business_And_Map_Page()
        {
            var fixture = new Fixture(BusinessStatus.Inactive);
            fixture
                .Movements.Setup(repository =>
                    repository.SearchHistoryByBusinessAsync(
                        fixture.BusinessId,
                        It.IsAny<FinancialMovementHistoryFilters>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    new FinancialMovementHistoryPageProjection(
                        [
                            new(
                                Guid.NewGuid(),
                                FinancialMovementType.Sale,
                                FinancialMovementStatus.Confirmed,
                                new DateOnly(2026, 10, 11),
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
            query.Page = 2;
            query.PageSize = 5;

            var result = await fixture.Handler().Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var item = result.Value.Items.Should().ContainSingle().Subject;
            item.Type.Should().Be(FinancialMovementType.Sale);
            item.Status.Should().Be(FinancialMovementStatus.Confirmed);
            item.MovementDate.Should().Be(new DateOnly(2026, 10, 11));
            item.Description.Should().Be("Venta");
            item.CurrencyCode.Should().Be("PEN");
            item.TotalAmount.Should().Be(10m);
            item.ActiveItemCount.Should().Be(2);
            item.HasActiveEvidence.Should().BeTrue();
            item.ConfirmedAt.Should().Be(fixture.Now);
            item.CancelledAt.Should().BeNull();
            item.DiscardedAt.Should().BeNull();
            result.Value.Page.Should().Be(2);
            result.Value.PageSize.Should().Be(5);
            result.Value.TotalCount.Should().Be(8);
            result.Value.HasPreviousPage.Should().BeTrue();
            result.Value.HasNextPage.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Should_Reject_Category_Outside_Business()
        {
            var fixture = new Fixture(BusinessStatus.Active);
            fixture
                .Categories.Setup(repository =>
                    repository.ExistsByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        fixture.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(false);
            var query = fixture.Query();
            query.CategoryId = Guid.NewGuid();

            var result = await fixture.Handler().Handle(query, CancellationToken.None);

            result.Error.Code.Should().Be(ErrorCodes.CategoryNotFound);
            fixture.Movements.Verify(
                repository =>
                    repository.SearchHistoryByBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<FinancialMovementHistoryFilters>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Theory]
        [InlineData(false, true, ErrorCodes.BusinessAccessForbidden)]
        [InlineData(true, false, ErrorCodes.BusinessAccessForbidden)]
        public async Task Handle_Should_Reject_Inactive_Membership_Or_Role(
            bool membershipActive,
            bool roleActive,
            string expectedCode
        )
        {
            var fixture = new Fixture(
                BusinessStatus.Active,
                membershipActive
                    ? BusinessMembershipStatus.Active
                    : BusinessMembershipStatus.Inactive,
                roleActive
            );

            var result = await fixture.Handler().Handle(fixture.Query(), CancellationToken.None);

            result.Error.Code.Should().Be(expectedCode);
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_HistoryRead()
        {
            var fixture = new Fixture(BusinessStatus.Active, permissions: []);

            var result = await fixture.Handler().Handle(fixture.Query(), CancellationToken.None);

            result.Error.Code.Should().Be(ErrorCodes.MovementAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Map_Unexpected_Error_And_Rethrow_Cancellation()
        {
            var failure = new Fixture(BusinessStatus.Active);
            failure
                .Movements.Setup(repository =>
                    repository.SearchHistoryByBusinessAsync(
                        failure.BusinessId,
                        It.IsAny<FinancialMovementHistoryFilters>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ThrowsAsync(new InvalidOperationException());

            var failed = await failure.Handler().Handle(failure.Query(), CancellationToken.None);
            failed.Error.Code.Should().Be(ErrorCodes.FinancialHistoryQueryFailed);

            var cancelled = new Fixture(BusinessStatus.Active);
            cancelled
                .Movements.Setup(repository =>
                    repository.SearchHistoryByBusinessAsync(
                        cancelled.BusinessId,
                        It.IsAny<FinancialMovementHistoryFilters>(),
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
                bool roleActive = true,
                string[] permissions = null
            )
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
                            status,
                            Guid.NewGuid(),
                            membership,
                            RoleId,
                            "OWNER",
                            roleActive
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
                Movements
                    .Setup(repository =>
                        repository.SearchHistoryByBusinessAsync(
                            BusinessId,
                            It.IsAny<FinancialMovementHistoryFilters>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(new FinancialMovementHistoryPageProjection([], 1, 20, 0));
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
