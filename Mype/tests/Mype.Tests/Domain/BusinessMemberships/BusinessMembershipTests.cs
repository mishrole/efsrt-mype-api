using FluentAssertions;
using Mype.Domain.BusinessMemberships;
using System;

namespace Mype.Tests.Domain.BusinessMemberships
{
    public class BusinessMembershipTests
    {
        private static readonly Guid BusinessId =
            Guid.NewGuid();

        private static readonly Guid UserId =
            Guid.NewGuid();

        private static readonly Guid RoleId =
            Guid.NewGuid();

        private static readonly DateTimeOffset CreatedAt =
            new(
                2026,
                10,
                9,
                12,
                0,
                0,
                TimeSpan.Zero
            );

        [Fact]
        public void CreateOwner_Should_Initialize_Active_Membership()
        {
            var membership = CreateMembership();

            membership.Id.Should().NotBeEmpty();
            membership.BusinessId.Should().Be(BusinessId);
            membership.UserId.Should().Be(UserId);
            membership.RoleId.Should().Be(RoleId);
            membership.Status.Should().Be(
                BusinessMembershipStatus.Active
            );
            membership.JoinedAt.Should().Be(CreatedAt);
            membership.DeactivatedAt.Should().BeNull();
            membership.ReactivatedAt.Should().BeNull();
            membership.CreatedByUserId.Should().Be(UserId);
            membership.UpdatedByUserId.Should().Be(UserId);
            membership.CreatedAt.Should().Be(CreatedAt);
            membership.UpdatedAt.Should().Be(CreatedAt);
            membership.IsActive().Should().BeTrue();
        }

        [Fact]
        public void Deactivate_Should_Set_Membership_As_Inactive()
        {
            var membership = CreateMembership();
            var currentUserId = Guid.NewGuid();
            var deactivatedAt = CreatedAt.AddHours(1);

            membership.Deactivate(
                currentUserId,
                deactivatedAt
            );

            membership.Status.Should().Be(
                BusinessMembershipStatus.Inactive
            );
            membership.DeactivatedAt.Should().Be(
                deactivatedAt
            );
            membership.UpdatedByUserId.Should().Be(
                currentUserId
            );
            membership.UpdatedAt.Should().Be(
                deactivatedAt
            );
            membership.IsActive().Should().BeFalse();
        }

        [Fact]
        public void Reactivate_Should_Set_Membership_As_Active()
        {
            var membership = CreateMembership();
            var currentUserId = Guid.NewGuid();
            var reactivatedAt = CreatedAt.AddHours(2);

            membership.Deactivate(
                currentUserId,
                CreatedAt.AddHours(1)
            );

            membership.Reactivate(
                currentUserId,
                reactivatedAt
            );

            membership.Status.Should().Be(
                BusinessMembershipStatus.Active
            );
            membership.DeactivatedAt.Should().BeNull();
            membership.ReactivatedAt.Should().Be(
                reactivatedAt
            );
            membership.UpdatedByUserId.Should().Be(
                currentUserId
            );
            membership.UpdatedAt.Should().Be(
                reactivatedAt
            );
            membership.IsActive().Should().BeTrue();
        }

        private static BusinessMembership
            CreateMembership()
        {
            return BusinessMembership.CreateOwner(
                BusinessId,
                UserId,
                RoleId,
                CreatedAt
            );
        }
    }
}
