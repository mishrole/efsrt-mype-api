using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.Businesses.Queries.ListUserBusinesses;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Domain.Businesses;

namespace Mype.Tests.Application.Businesses.Queries.ListUserBusinesses
{
    public class ListUserBusinessesQueryHandlerTests
    {
        private const string FirstBusinessName = "Bodega Central";

        private const string SecondBusinessName = "Distribuidora Norte";

        private const string CurrencyCode = "PEN";

        private const string OwnerRoleCode = "OWNER";

        private const string CollaboratorRoleCode = "COLLABORATOR";

        private static readonly Guid CurrentUserId = Guid.NewGuid();

        private static readonly Guid FirstBusinessId = Guid.NewGuid();

        private static readonly Guid SecondBusinessId = Guid.NewGuid();

        private static readonly Guid FirstMembershipId = Guid.NewGuid();

        private static readonly Guid SecondMembershipId = Guid.NewGuid();

        private readonly Mock<IBusinessMembershipRepository> _membershipRepositoryMock = new();

        private readonly ListUserBusinessesQueryHandler _handler;

        public ListUserBusinessesQueryHandlerTests()
        {
            _handler = new ListUserBusinessesQueryHandler(_membershipRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Return_Mapped_Businesses_When_Repository_Returns_Active_Memberships()
        {
            IReadOnlyCollection<BusinessSummaryProjection> projections =
            [
                new BusinessSummaryProjection(
                    FirstBusinessId,
                    FirstBusinessName,
                    CurrencyCode,
                    BusinessStatus.Active,
                    FirstMembershipId,
                    OwnerRoleCode
                ),
                new BusinessSummaryProjection(
                    SecondBusinessId,
                    SecondBusinessName,
                    CurrencyCode,
                    BusinessStatus.Active,
                    SecondMembershipId,
                    CollaboratorRoleCode
                ),
            ];

            SetupRepository(projections);

            var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.Should().NotBeNull();
            result.Value.Should().HaveCount(2);

            var businesses = result.Value.ToArray();

            AssertBusiness(
                businesses[0],
                FirstBusinessId,
                FirstBusinessName,
                FirstMembershipId,
                OwnerRoleCode
            );

            AssertBusiness(
                businesses[1],
                SecondBusinessId,
                SecondBusinessName,
                SecondMembershipId,
                CollaboratorRoleCode
            );

            VerifyRepositoryWasCalled();
        }

        [Fact]
        public async Task Handle_Should_Return_Empty_Collection_When_User_Has_No_Available_Businesses()
        {
            SetupRepository(Array.Empty<BusinessSummaryProjection>());

            var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEmpty();

            VerifyRepositoryWasCalled();
        }

        [Fact]
        public async Task Handle_Should_Use_Current_User_Id_When_Querying_Memberships()
        {
            SetupRepository(Array.Empty<BusinessSummaryProjection>());

            await _handler.Handle(CreateQuery(), CancellationToken.None);

            VerifyRepositoryWasCalled();
        }

        [Fact]
        public async Task Handle_Should_Forward_CancellationToken_To_Repository()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            _membershipRepositoryMock
                .Setup(repository =>
                    repository.ListActiveByUserIdAsync(CurrentUserId, cancellationToken)
                )
                .ReturnsAsync(Array.Empty<BusinessSummaryProjection>());

            await _handler.Handle(CreateQuery(), cancellationToken);

            _membershipRepositoryMock.Verify(
                repository => repository.ListActiveByUserIdAsync(CurrentUserId, cancellationToken),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Propagate_OperationCanceledException()
        {
            _membershipRepositoryMock
                .Setup(repository =>
                    repository.ListActiveByUserIdAsync(CurrentUserId, It.IsAny<CancellationToken>())
                )
                .ThrowsAsync(new OperationCanceledException());

            var action = async () => await _handler.Handle(CreateQuery(), CancellationToken.None);

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        private void SetupRepository(IReadOnlyCollection<BusinessSummaryProjection> projections)
        {
            _membershipRepositoryMock
                .Setup(repository =>
                    repository.ListActiveByUserIdAsync(CurrentUserId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(projections);
        }

        private void VerifyRepositoryWasCalled()
        {
            _membershipRepositoryMock.Verify(
                repository =>
                    repository.ListActiveByUserIdAsync(
                        CurrentUserId,
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        private static void AssertBusiness(
            BusinessSummaryResult result,
            Guid businessId,
            string displayName,
            Guid membershipId,
            string roleCode
        )
        {
            result.BusinessId.Should().Be(businessId);

            result.DisplayName.Should().Be(displayName);

            result.CurrencyCode.Should().Be(CurrencyCode);

            result.Status.Should().Be(BusinessStatus.Active);

            result.MembershipId.Should().Be(membershipId);

            result.RoleCode.Should().Be(roleCode);
        }

        private static ListUserBusinessesQuery CreateQuery()
        {
            return new ListUserBusinessesQuery { CurrentUserId = CurrentUserId };
        }
    }
}
