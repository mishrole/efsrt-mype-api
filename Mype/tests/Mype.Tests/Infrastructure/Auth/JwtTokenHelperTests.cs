using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Mype.Infrastructure.Auth;
using Mype.Shared.Constants;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Mype.Tests.Infrastructure.Auth
{
    public class JwtTokenHelperTests
    {
        private const string SecretKey = "test-secret-key-with-at-least-32-characters";

        private const string Issuer = "mype-tests";
        private const string Audience = "mype-tests-client";
        private const string ExpirationMinutes = "60";
        private const string Email = "user@example.com";
        private const string DisplayName = "Test User";
        private const string TokenType = AuthConstants.TokenType;

        private static readonly Guid UserId = Guid.NewGuid();

        private static readonly DateTimeOffset IssuedAt =
            new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void GenerateToken_Should_Return_Token_Metadata()
        {
            var helper = CreateHelper();

            var result = helper.GenerateToken(
                UserId,
                Email,
                DisplayName,
                IssuedAt
            );

            result.Value.Should().NotBeNullOrWhiteSpace();
            result.TokenType.Should().Be(TokenType);
            result.IssuedAt.Should().Be(IssuedAt);
            result.ExpiresAt.Should().Be(
                IssuedAt.AddMinutes(60)
            );
        }

        [Fact]
        public void GenerateToken_Should_Include_Required_Claims()
        {
            var helper = CreateHelper();

            var result = helper.GenerateToken(
                UserId,
                Email,
                DisplayName,
                IssuedAt
            );

            using var payload = ReadPayload(result.Value);

            payload.RootElement
                .GetProperty("sub")
                .GetString()
                .Should()
                .Be(UserId.ToString());

            payload.RootElement
                .GetProperty("email")
                .GetString()
                .Should()
                .Be(Email);

            payload.RootElement
                .GetProperty("name")
                .GetString()
                .Should()
                .Be(DisplayName);

            payload.RootElement
                .GetProperty("jti")
                .GetString()
                .Should()
                .NotBeNullOrWhiteSpace();

            payload.RootElement
                .TryGetProperty("iat", out _)
                .Should()
                .BeTrue();

            payload.RootElement
                .TryGetProperty("exp", out _)
                .Should()
                .BeTrue();

            payload.RootElement
                .GetProperty("iss")
                .GetString()
                .Should()
                .Be(Issuer);

            GetAudience(payload.RootElement)
                .Should()
                .Contain(Audience);
        }

        [Fact]
        public void GenerateToken_Should_Use_Configured_Expiration()
        {
            const string configuredExpiration = "45";

            var helper = CreateHelper(
                expirationMinutes: configuredExpiration
            );

            var result = helper.GenerateToken(
                UserId,
                Email,
                DisplayName,
                IssuedAt
            );

            result.ExpiresAt.Should().Be(
                IssuedAt.AddMinutes(45)
            );

            using var payload = ReadPayload(result.Value);

            var issuedAtUnix = payload.RootElement
                .GetProperty("iat")
                .GetInt64();

            var expiresAtUnix = payload.RootElement
                .GetProperty("exp")
                .GetInt64();

            expiresAtUnix
                .Should()
                .Be(issuedAtUnix + 45 * 60);
        }

        [Fact]
        public void GenerateToken_Should_Not_Include_Business_Or_Sensitive_Claims()
        {
            var helper = CreateHelper();

            var result = helper.GenerateToken(
                UserId,
                Email,
                DisplayName,
                IssuedAt
            );

            using var payload = ReadPayload(result.Value);

            payload.RootElement
                .TryGetProperty("businessId", out _)
                .Should()
                .BeFalse();

            payload.RootElement
                .TryGetProperty("businessMembershipId", out _)
                .Should()
                .BeFalse();

            payload.RootElement
                .TryGetProperty("businessRole", out _)
                .Should()
                .BeFalse();

            payload.RootElement
                .TryGetProperty("passwordHash", out _)
                .Should()
                .BeFalse();
        }

        [Theory]
        [InlineData(Env.JwtSecretKeyStringKey)]
        [InlineData(Env.JwtIssuerStringKey)]
        [InlineData(Env.JwtAudienceStringKey)]
        [InlineData(Env.JwtExpirationMinutesStringKey)]
        public void GenerateToken_Should_Throw_When_Required_Configuration_Is_Missing(
            string missingConfigurationKey
        )
        {
            var configurationValues = CreateConfigurationValues();

            configurationValues.Remove(
                missingConfigurationKey
            );

            var helper = CreateHelper(configurationValues);

            var action = () => helper.GenerateToken(
                UserId,
                Email,
                DisplayName,
                IssuedAt
            );

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage(
                    $"*{missingConfigurationKey}*"
                );
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("0")]
        [InlineData("-1")]
        public void GenerateToken_Should_Throw_When_Expiration_Is_Invalid(
            string invalidExpiration
        )
        {
            var helper = CreateHelper(
                expirationMinutes: invalidExpiration
            );

            var action = () => helper.GenerateToken(
                UserId,
                Email,
                DisplayName,
                IssuedAt
            );

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage(
                    $"*{Env.JwtExpirationMinutesStringKey}*"
                );
        }

        private static JwtTokenHelper CreateHelper(
            string expirationMinutes = ExpirationMinutes
        )
        {
            var configurationValues = CreateConfigurationValues();

            configurationValues[
                Env.JwtExpirationMinutesStringKey
            ] = expirationMinutes;

            return CreateHelper(configurationValues);
        }

        private static JwtTokenHelper CreateHelper(
            Dictionary<string, string> configurationValues
        )
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationValues)
                .Build();

            return new JwtTokenHelper(configuration);
        }

        private static Dictionary<string, string>
            CreateConfigurationValues()
        {
            return new Dictionary<string, string>
            {
                [Env.JwtSecretKeyStringKey] = SecretKey,
                [Env.JwtIssuerStringKey] = Issuer,
                [Env.JwtAudienceStringKey] = Audience,
                [Env.JwtExpirationMinutesStringKey] =
                    ExpirationMinutes
            };
        }

        private static JsonDocument ReadPayload(
            string token
        )
        {
            var sections = token.Split('.');

            sections.Should().HaveCount(3);

            var payloadBytes = DecodeBase64Url(
                sections[1]
            );

            return JsonDocument.Parse(payloadBytes);
        }

        private static byte[] DecodeBase64Url(
            string value
        )
        {
            var base64 = value
                .Replace('-', '+')
                .Replace('_', '/');

            base64 = base64.PadRight(
                base64.Length + ((4 - base64.Length % 4) % 4),
                '='
            );

            return Convert.FromBase64String(base64);
        }

        private static string[] GetAudience(
            JsonElement payload
        )
        {
            var audience = payload.GetProperty("aud");

            if (audience.ValueKind == JsonValueKind.Array)
            {
                var values = new List<string>();

                foreach (var value in audience.EnumerateArray())
                {
                    values.Add(
                        value.GetString() ?? string.Empty
                    );
                }

                return values.ToArray();
            }

            return
            [
                audience.GetString() ?? string.Empty
            ];
        }
    }
}