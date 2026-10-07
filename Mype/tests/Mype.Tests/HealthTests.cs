using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mype.Tests
{
    public class HealthTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public HealthTests(
            WebApplicationFactory<Program> factory
        )
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Get_Should_Return_Success()
        {
            var response = await _client.GetAsync("/health");

            var body = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Status: {(int)response.StatusCode}, Body: {body}");

            response.IsSuccessStatusCode.Should().BeTrue();
        }
    }
}
