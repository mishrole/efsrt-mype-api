using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Infrastructure.Persistence;
using Mype.Shared.Constants;
using Mype.Tests.Integration;

namespace Mype.Tests.Integration.Auth
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class RegisterUserIntegrationTests
    {
        private const string Password = "password1";
        private readonly PostgreSqlFixture _database;

        public RegisterUserIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task Register_Should_Return_Created_And_Persist_User()
        {
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var email = UniqueEmail();

            var response = await client.PostAsJsonAsync(
                "/api/v1/auth/register",
                Request("Usuario de integración", email)
            );

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location.Should().NotBeNull();

            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var user = await context.Users.AsNoTracking().SingleAsync(item => item.NormalizedEmail == email.ToUpperInvariant());
            user.Email.Should().Be(email);
            user.DisplayName.Should().Be("Usuario de integración");
            user.PasswordHash.Should().NotBe(Password);
        }

        [Fact]
        public async Task Register_Should_Return_Validation_Error_And_Not_Persist_User()
        {
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var email = UniqueEmail();

            var response = await client.PostAsJsonAsync(
                "/api/v1/auth/register",
                Request(string.Empty, email, "invalid", "different")
            );

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ReadCode(response)).Should().Be(ErrorCodes.ValidationError);

            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            (await context.Users.AsNoTracking().CountAsync(item => item.NormalizedEmail == email.ToUpperInvariant())).Should().Be(0);
        }

        [Fact]
        public async Task Register_Should_Return_Conflict_When_Email_Already_Exists()
        {
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var email = UniqueEmail();

            (await client.PostAsJsonAsync("/api/v1/auth/register", Request("Primero", email))).StatusCode.Should().Be(HttpStatusCode.Created);
            var duplicate = await client.PostAsJsonAsync("/api/v1/auth/register", Request("Segundo", email));

            duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ReadCode(duplicate)).Should().Be(ErrorCodes.EmailAlreadyRegistered);
        }

        [Fact]
        public async Task Register_Should_Treat_Whitespace_And_Casing_As_Equivalent_Email()
        {
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var localPart = $"integration-{Guid.NewGuid():N}";
            var canonical = $"{localPart}@example.com";
            var equivalent = $"  {localPart.ToUpperInvariant()}@EXAMPLE.COM  ";

            (await client.PostAsJsonAsync("/api/v1/auth/register", Request("Primero", canonical))).StatusCode.Should().Be(HttpStatusCode.Created);
            var duplicate = await client.PostAsJsonAsync("/api/v1/auth/register", Request("Segundo", equivalent));

            duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ReadCode(duplicate)).Should().Be(ErrorCodes.EmailAlreadyRegistered);
        }

        [Fact]
        public async Task Register_Should_Persist_One_User_When_Equivalent_Requests_Are_Concurrent()
        {
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var email = UniqueEmail();

            var responses = await Task.WhenAll(
                client.PostAsJsonAsync("/api/v1/auth/register", Request("Concurrente A", email)),
                client.PostAsJsonAsync("/api/v1/auth/register", Request("Concurrente B", email.ToUpperInvariant()))
            );

            responses.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(1);
            responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
            (await ReadCode(responses.Single(response => response.StatusCode == HttpStatusCode.Conflict))).Should().Be(ErrorCodes.EmailAlreadyRegistered);

            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            (await context.Users.AsNoTracking().CountAsync(item => item.NormalizedEmail == email.ToUpperInvariant())).Should().Be(1);
        }

        private MypeWebApplicationFactory CreateFactory() => new(_database.ConnectionString);

        private static object Request(string displayName, string email, string password = Password, string confirmation = Password) =>
            new { displayName, email, password, passwordConfirmation = confirmation };

        private static string UniqueEmail() => $"integration-{Guid.NewGuid():N}@example.com";

        private static async Task<string> ReadCode(System.Net.Http.HttpResponseMessage response)
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return document.RootElement.GetProperty("code").GetString();
        }
    }
}
