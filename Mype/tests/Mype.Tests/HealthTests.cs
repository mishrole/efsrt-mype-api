using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Mype.Infrastructure.Persistence;
using Mype.Shared.Constants;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Mype.Tests
{
    public class HealthTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;
        private readonly WebApplicationFactory<Program> _factory;

        public HealthTests(
            WebApplicationFactory<Program> factory,
            ITestOutputHelper output
        )
        {
            _client = factory.CreateClient();
            _output = output;
            _factory = factory;
        }

        [Fact]
        public async Task Get_Should_Return_Success()
        {
            var response = await _client.GetAsync("/health");

            var body = await response.Content.ReadAsStringAsync();

            _output.WriteLine($"Status: {(int)response.StatusCode}, Body: {body}");

            _output.WriteLine($"Length: {Environment.GetEnvironmentVariable(Env.ConnectionStringKey)?.Length ?? 0}");

            await using var scope = _factory.Services.CreateAsyncScope();

            var db = scope.ServiceProvider.GetRequiredService<MypeDbContext>();

            try
            {
                var canConnect = await db.Database.CanConnectAsync();
                _output.WriteLine($"Can connect to database: {canConnect}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error connecting to database: {ex.ToString()}");
            }

            response.IsSuccessStatusCode.Should().BeTrue();
        }
    }
}
