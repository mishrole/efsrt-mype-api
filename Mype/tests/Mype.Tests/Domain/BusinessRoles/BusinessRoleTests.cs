using System;
using FluentAssertions;
using Mype.Domain.BusinessRoles;

namespace Mype.Tests.Domain.BusinessRoles
{
    public class BusinessRoleTests
    {
        [Fact]
        public void CreateSystem_Should_Initialize_Active_System_Role()
        {
            var id = Guid.NewGuid();
            var utcNow = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

            var role = BusinessRole.CreateSystem(
                id,
                "OWNER",
                "Propietario",
                "Administra el negocio.",
                utcNow
            );

            role.Id.Should().Be(id);
            role.Code.Should().Be("OWNER");
            role.Name.Should().Be("Propietario");
            role.Description.Should().Be("Administra el negocio.");
            role.IsSystem.Should().BeTrue();
            role.IsActive.Should().BeTrue();
            role.CreatedAt.Should().Be(utcNow);
            role.UpdatedAt.Should().Be(utcNow);
        }
    }
}
