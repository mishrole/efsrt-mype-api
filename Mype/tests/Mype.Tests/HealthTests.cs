using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Mype.Tests
{
    public class HealthTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public HealthTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Get_Should_Return_Success()
        {
            var response = await _client.GetAsync("/health");

            response.IsSuccessStatusCode.Should().BeTrue();
        }
    }
}
