using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Domain.BusinessRoles;
using Mype.Infrastructure.BusinessRoles.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.BusinessRoles.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class BusinessRoleRepositoryIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public BusinessRoleRepositoryIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task GetActiveByCodeAsync_Should_Return_Detached_Active_Role()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var expected = await context
                .BusinessRoles.AsNoTracking()
                .FirstAsync(item => item.IsActive);
            var repository = new BusinessRoleRepository(context);

            var result = await repository.GetActiveByCodeAsync(
                expected.Code,
                CancellationToken.None
            );

            result.Should().NotBeNull();
            result.Id.Should().Be(expected.Id);
            context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetActiveByCodeAsync_Should_Return_Null_For_Unknown_Code()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var repository = new BusinessRoleRepository(
                scope.ServiceProvider.GetRequiredService<MypeDbContext>()
            );

            (await repository.GetActiveByCodeAsync("UNKNOWN_ROLE", CancellationToken.None))
                .Should()
                .BeNull();
        }
    }
}
