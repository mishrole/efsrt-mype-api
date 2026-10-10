using FluentAssertions;
using Mype.Domain.Permissions;
using System;

namespace Mype.Tests.Domain.Permissions
{
    public class PermissionTests
    {
        [Fact]
        public void CreateSystem_Should_Initialize_Active_System_Permission()
        {
            var id = Guid.NewGuid();
            var utcNow = new DateTimeOffset(
                2026,
                10,
                9,
                12,
                0,
                0,
                TimeSpan.Zero
            );

            var permission = Permission.CreateSystem(
                id,
                "PRODUCT_READ",
                "Consultar productos",
                "Permite consultar productos.",
                utcNow
            );

            permission.Id.Should().Be(id);
            permission.Code.Should().Be("PRODUCT_READ");
            permission.Name.Should().Be(
                "Consultar productos"
            );
            permission.Description.Should().Be(
                "Permite consultar productos."
            );
            permission.IsSystem.Should().BeTrue();
            permission.IsActive.Should().BeTrue();
            permission.CreatedAt.Should().Be(utcNow);
            permission.UpdatedAt.Should().Be(utcNow);
        }
    }
}
