using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Infrastructure.Persistence;
using Mype.Tests.Integration;

namespace Mype.Tests
{
    [Collection(PostgreSqlCollection.Name)]
    public class HealthTests
    {
        private readonly PostgreSqlFixture _database;

        public HealthTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task Get_Should_Apply_Migrations_And_Return_Success()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/health");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
            (await context.Database.CanConnectAsync()).Should().BeTrue();
        }
    }
}
