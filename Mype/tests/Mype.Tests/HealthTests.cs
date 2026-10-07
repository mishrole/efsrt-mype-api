using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
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

        public HealthTests(
            WebApplicationFactory<Program> factory,
            ITestOutputHelper output
        )
        {
            _client = factory.CreateClient();
            _output = output;
        }

        [Fact]
        public async Task Get_Should_Return_Success()
        {
            var response = await _client.GetAsync("/health");

            var body = await response.Content.ReadAsStringAsync();

            _output.WriteLine($"Status: {(int)response.StatusCode}, Body: {body}");

            _output.WriteLine($"Length: {Environment.GetEnvironmentVariable(Env.ConnectionStringKey)?.Length ?? 0}");

            response.IsSuccessStatusCode.Should().BeTrue();
        }
    }
}
