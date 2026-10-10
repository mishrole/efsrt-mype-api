using FluentAssertions;
using Mype.Domain.BusinessRolePermissions;
using System;

namespace Mype.Tests.Domain.BusinessRolePermissions
{
    public class BusinessRolePermissionTests
    {
        [Fact]
        public void Create_Should_Initialize_Relation()
        {
            var roleId = Guid.NewGuid();
            var permissionId = Guid.NewGuid();
            var createdAt = new DateTimeOffset(
                2026,
                10,
                9,
                12,
                0,
                0,
                TimeSpan.Zero
            );

            var relation =
                BusinessRolePermission.Create(
                    roleId,
                    permissionId,
                    createdAt
                );

            relation.BusinessRoleId.Should().Be(roleId);
            relation.PermissionId.Should().Be(
                permissionId
            );
            relation.CreatedAt.Should().Be(createdAt);
        }
    }
}
