using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Mype.Infrastructure.Auth;
using Mype.Shared.Constants;

namespace Mype.Tests.Infrastructure.Auth
{
    public class JwtTokenValidationTests
    {
        private const string SecretKey = "test-secret-key-with-at-least-32-characters";

        private const string DifferentSecretKey = "different-test-key-with-at-least-32-chars";

        private const string Issuer = "mype-tests";
        private const string Audience = "mype-tests-client";
        private const string Email = "user@example.com";
        private const string DisplayName = "Test User";
        private const string ExpirationMinutes = "60";

        private static readonly Guid UserId = Guid.NewGuid();

        [Fact]
        public async Task ValidateToken_Should_Succeed_When_Token_Is_Valid()
        {
            var token = GenerateToken(DateTimeOffset.UtcNow);

            var result = await ValidateTokenAsync(token);

            result.IsValid.Should().BeTrue();
            result.Exception.Should().BeNull();
        }

        [Fact]
        public async Task ValidateToken_Should_Fail_When_Token_Is_Expired()
        {
            var issuedAt = DateTimeOffset.UtcNow.AddHours(-2);

            var token = GenerateToken(issuedAt, expirationMinutes: "1");

            var result = await ValidateTokenAsync(token);

            result.IsValid.Should().BeFalse();

            result.Exception.Should().BeOfType<SecurityTokenExpiredException>();
        }

        [Fact]
        public async Task ValidateToken_Should_Fail_When_Signature_Is_Manipulated()
        {
            var token = GenerateToken(DateTimeOffset.UtcNow);

            var manipulatedToken = ManipulateSignature(token);

            var result = await ValidateTokenAsync(manipulatedToken);

            result.IsValid.Should().BeFalse();

            result.Exception.Should().BeAssignableTo<SecurityTokenInvalidSignatureException>();
        }

        [Fact]
        public async Task ValidateToken_Should_Fail_When_Issuer_Is_Incorrect()
        {
            var token = GenerateToken(DateTimeOffset.UtcNow, issuer: "invalid-issuer");

            var result = await ValidateTokenAsync(token);

            result.IsValid.Should().BeFalse();

            result.Exception.Should().BeOfType<SecurityTokenInvalidIssuerException>();
        }

        [Fact]
        public async Task ValidateToken_Should_Fail_When_Audience_Is_Incorrect()
        {
            var token = GenerateToken(DateTimeOffset.UtcNow, audience: "invalid-audience");

            var result = await ValidateTokenAsync(token);

            result.IsValid.Should().BeFalse();

            result.Exception.Should().BeOfType<SecurityTokenInvalidAudienceException>();
        }

        [Fact]
        public async Task ValidateToken_Should_Fail_When_Signed_With_Different_Key()
        {
            var token = GenerateToken(DateTimeOffset.UtcNow, secretKey: DifferentSecretKey);

            var result = await ValidateTokenAsync(token);

            result.IsValid.Should().BeFalse();

            result.Exception.Should().BeAssignableTo<SecurityTokenInvalidSignatureException>();
        }

        private static string GenerateToken(
            DateTimeOffset issuedAt,
            string secretKey = SecretKey,
            string issuer = Issuer,
            string audience = Audience,
            string expirationMinutes = ExpirationMinutes
        )
        {
            var helper = CreateHelper(secretKey, issuer, audience, expirationMinutes);

            return helper.GenerateToken(UserId, Email, DisplayName, issuedAt).Value;
        }

        private static JwtTokenHelper CreateHelper(
            string secretKey,
            string issuer,
            string audience,
            string expirationMinutes
        )
        {
            var values = new Dictionary<string, string>
            {
                [Env.JwtSecretKeyStringKey] = secretKey,
                [Env.JwtIssuerStringKey] = issuer,
                [Env.JwtAudienceStringKey] = audience,
                [Env.JwtExpirationMinutesStringKey] = expirationMinutes,
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

            return new JwtTokenHelper(configuration);
        }

        private static Task<TokenValidationResult> ValidateTokenAsync(string token)
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = Issuer,
                ValidAudience = Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
                ClockSkew = TimeSpan.Zero,
            };

            var tokenHandler = new JsonWebTokenHandler();

            return tokenHandler.ValidateTokenAsync(token, validationParameters);
        }

        private static string ManipulateSignature(string token)
        {
            var sections = token.Split('.');

            sections.Should().HaveCount(3);
            sections[2].Should().NotBeNullOrEmpty();

            var signature = sections[2].ToCharArray();

            signature[0] = signature[0] == 'A' ? 'B' : 'A';

            sections[2] = new string(signature);

            return string.Join(".", sections);
        }
    }
}
