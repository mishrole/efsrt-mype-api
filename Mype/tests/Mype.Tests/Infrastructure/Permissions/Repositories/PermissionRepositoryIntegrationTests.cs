using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Infrastructure.Permissions.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.Permissions.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class PermissionRepositoryIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public PermissionRepositoryIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task ListActiveCodesByRoleIdAsync_Should_Return_Sorted_Active_Codes()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var roleId = await context
                .BusinessRolePermissions.AsNoTracking()
                .Select(item => item.BusinessRoleId)
                .FirstAsync();
            var expected = await (
                from relation in context.BusinessRolePermissions.AsNoTracking()
                join permission in context.Permissions.AsNoTracking()
                    on relation.PermissionId equals permission.Id
                where relation.BusinessRoleId == roleId && permission.IsActive
                orderby permission.Code
                select permission.Code
            ).ToArrayAsync();
            var repository = new PermissionRepository(context);

            var result = await repository.ListActiveCodesByRoleIdAsync(
                roleId,
                CancellationToken.None
            );

            result.Should().Equal(expected);
        }

        [Fact]
        public async Task ListActiveCodesByRoleIdAsync_Should_Return_Empty_For_Unknown_Role()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var repository = new PermissionRepository(
                scope.ServiceProvider.GetRequiredService<MypeDbContext>()
            );

            (
                await repository.ListActiveCodesByRoleIdAsync(
                    System.Guid.NewGuid(),
                    CancellationToken.None
                )
            )
                .Should()
                .BeEmpty();
        }
    }
}
