using System;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace Mype.Tests.Integration
{
    public sealed class PostgreSqlFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(
            "postgres:18-alpine"
        )
            .WithDatabase("mype_tests")
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .WithCleanUp(true)
            .Build();

        public string ConnectionString => _container.GetConnectionString();

        public Task InitializeAsync() => _container.StartAsync();

        public Task DisposeAsync() => _container.DisposeAsync().AsTask();
    }
}
