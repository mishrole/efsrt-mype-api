using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.Businesses.Queries.GetBusinessContext;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Permissions.Constants;

namespace Mype.Tests.Application.Businesses.Queries.GetBusinessContext
{
    public class GetBusinessContextQueryHandlerTests
    {
        private const string DisplayName = "Bodega Central";

        private const string CurrencyCode = "PEN";

        private const string RoleCode = "OWNER";

        private static readonly Guid CurrentUserId = Guid.NewGuid();

        private static readonly Guid BusinessId = Guid.NewGuid();

        private static readonly Guid MembershipId = Guid.NewGuid();

        private static readonly Guid RoleId = Guid.NewGuid();

        private readonly Mock<IBusinessMembershipRepository> _membershipRepositoryMock = new();

        private readonly Mock<IPermissionRepository> _permissionRepositoryMock = new();

        private readonly GetBusinessContextQueryHandler _handler;

        public GetBusinessContextQueryHandlerTests()
        {
            _handler = new GetBusinessContextQueryHandler(
                _membershipRepositoryMock.Object,
                _permissionRepositoryMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return_Context_And_Active_Permissions_When_Access_Is_Valid()
        {
            var context = CreateContext();

            IReadOnlyCollection<string> permissions =
            [
                SystemPermissions.BusinessRead.Code,
                SystemPermissions.CategoryRead.Code,
                SystemPermissions.ProductRead.Code,
            ];

            SetupContext(context);
            SetupPermissions(permissions);

            var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.Should().NotBeNull();

            result.Value.BusinessId.Should().Be(BusinessId);

            result.Value.DisplayName.Should().Be(DisplayName);

            result.Value.CurrencyCode.Should().Be(CurrencyCode);

            result.Value.MembershipId.Should().Be(MembershipId);

            result.Value.RoleId.Should().Be(RoleId);

            result.Value.RoleCode.Should().Be(RoleCode);

            result.Value.Permissions.Should().Equal(permissions);

            VerifyContextWasQueried();
            VerifyPermissionsWereQueried();
        }

        [Fact]
        public async Task Handle_Should_Return_BusinessAccessForbidden_When_Membership_Does_Not_Exist()
        {
            SetupContext(null);

            var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

            AssertAccessForbidden(result);
            VerifyPermissionsWereNotQueried();
        }

        [Fact]
        public async Task Handle_Should_Return_BusinessAccessForbidden_When_Membership_Is_Inactive()
        {
            SetupContext(CreateContext(membershipStatus: BusinessMembershipStatus.Inactive));

            var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

            AssertAccessForbidden(result);
            VerifyPermissionsWereNotQueried();
        }

        [Fact]
        public async Task Handle_Should_Return_BusinessAccessForbidden_When_Role_Is_Inactive()
        {
            SetupContext(CreateContext(roleIsActive: false));

            var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

            AssertAccessForbidden(result);
            VerifyPermissionsWereNotQueried();
        }

        [Fact]
        public async Task Handle_Should_Return_BusinessUnavailable_When_Business_Is_Inactive()
        {
            SetupContext(CreateContext(businessStatus: BusinessStatus.Inactive));

            var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();

            result.Error.Should().BeSameAs(GetBusinessContextErrors.BusinessUnavailable);

            VerifyPermissionsWereNotQueried();
        }

        [Fact]
        public async Task Handle_Should_Return_Empty_Permissions_When_Role_Has_No_Active_Permissions()
        {
            SetupContext(CreateContext());

            SetupPermissions(Array.Empty<string>());

            var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.Should().NotBeNull();
            result.Value.Permissions.Should().BeEmpty();

            VerifyPermissionsWereQueried();
        }

        [Fact]
        public async Task Handle_Should_Not_Reveal_Business_Status_When_Membership_Is_Inactive()
        {
            SetupContext(
                CreateContext(
                    businessStatus: BusinessStatus.Inactive,
                    membershipStatus: BusinessMembershipStatus.Inactive
                )
            );

            var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

            result.Error.Should().BeSameAs(GetBusinessContextErrors.BusinessAccessForbidden);

            result.Error.Should().NotBeSameAs(GetBusinessContextErrors.BusinessUnavailable);

            VerifyPermissionsWereNotQueried();
        }

        [Fact]
        public async Task Handle_Should_Not_Reveal_Business_Status_When_Role_Is_Inactive()
        {
            SetupContext(
                CreateContext(businessStatus: BusinessStatus.Inactive, roleIsActive: false)
            );

            var result = await _handler.Handle(CreateQuery(), CancellationToken.None);

            result.Error.Should().BeSameAs(GetBusinessContextErrors.BusinessAccessForbidden);

            result.Error.Should().NotBeSameAs(GetBusinessContextErrors.BusinessUnavailable);

            VerifyPermissionsWereNotQueried();
        }

        [Fact]
        public async Task Handle_Should_Use_Business_And_Current_User_When_Querying_Context()
        {
            SetupContext(CreateContext());

            SetupPermissions(Array.Empty<string>());

            await _handler.Handle(CreateQuery(), CancellationToken.None);

            VerifyContextWasQueried();
        }

        [Fact]
        public async Task Handle_Should_Forward_CancellationToken_To_Repositories()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            _membershipRepositoryMock
                .Setup(repository =>
                    repository.GetContextByBusinessAndUserAsync(
                        BusinessId,
                        CurrentUserId,
                        cancellationToken
                    )
                )
                .ReturnsAsync(CreateContext());

            _permissionRepositoryMock
                .Setup(repository =>
                    repository.ListActiveCodesByRoleIdAsync(RoleId, cancellationToken)
                )
                .ReturnsAsync(Array.Empty<string>());

            await _handler.Handle(CreateQuery(), cancellationToken);

            _membershipRepositoryMock.Verify(
                repository =>
                    repository.GetContextByBusinessAndUserAsync(
                        BusinessId,
                        CurrentUserId,
                        cancellationToken
                    ),
                Times.Once
            );

            _permissionRepositoryMock.Verify(
                repository => repository.ListActiveCodesByRoleIdAsync(RoleId, cancellationToken),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Propagate_OperationCanceledException_From_Context_Repository()
        {
            _membershipRepositoryMock
                .Setup(repository =>
                    repository.GetContextByBusinessAndUserAsync(
                        BusinessId,
                        CurrentUserId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ThrowsAsync(new OperationCanceledException());

            var action = async () => await _handler.Handle(CreateQuery(), CancellationToken.None);

            await action.Should().ThrowAsync<OperationCanceledException>();

            VerifyPermissionsWereNotQueried();
        }

        [Fact]
        public async Task Handle_Should_Propagate_OperationCanceledException_From_Permission_Repository()
        {
            SetupContext(CreateContext());

            _permissionRepositoryMock
                .Setup(repository =>
                    repository.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>())
                )
                .ThrowsAsync(new OperationCanceledException());

            var action = async () => await _handler.Handle(CreateQuery(), CancellationToken.None);

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        private void SetupContext(BusinessContextProjection context)
        {
            _membershipRepositoryMock
                .Setup(repository =>
                    repository.GetContextByBusinessAndUserAsync(
                        BusinessId,
                        CurrentUserId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(context);
        }

        private void SetupPermissions(IReadOnlyCollection<string> permissions)
        {
            _permissionRepositoryMock
                .Setup(repository =>
                    repository.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(permissions);
        }

        private void VerifyContextWasQueried()
        {
            _membershipRepositoryMock.Verify(
                repository =>
                    repository.GetContextByBusinessAndUserAsync(
                        BusinessId,
                        CurrentUserId,
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        private void VerifyPermissionsWereQueried()
        {
            _permissionRepositoryMock.Verify(
                repository =>
                    repository.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        private void VerifyPermissionsWereNotQueried()
        {
            _permissionRepositoryMock.Verify(
                repository =>
                    repository.ListActiveCodesByRoleIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        private static void AssertAccessForbidden(
            Mype.Application.Common.Result<BusinessContextResult> result
        )
        {
            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();

            result.Error.Should().BeSameAs(GetBusinessContextErrors.BusinessAccessForbidden);
        }

        private static BusinessContextProjection CreateContext(
            BusinessStatus businessStatus = BusinessStatus.Active,
            BusinessMembershipStatus membershipStatus = BusinessMembershipStatus.Active,
            bool roleIsActive = true
        )
        {
            return new BusinessContextProjection(
                BusinessId,
                DisplayName,
                CurrencyCode,
                businessStatus,
                MembershipId,
                membershipStatus,
                RoleId,
                RoleCode,
                roleIsActive
            );
        }

        private static GetBusinessContextQuery CreateQuery()
        {
            return new GetBusinessContextQuery
            {
                BusinessId = BusinessId,
                CurrentUserId = CurrentUserId,
            };
        }
    }
}
