using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Mype.Infrastructure.Auth;
using Mype.Shared.Constants;

namespace Mype.Tests.Infrastructure.Auth
{
    public class RefreshTokenHelperTests
    {
        private const string Pepper = "a-test-refresh-token-pepper-that-is-not-the-jwt-key";
        private static readonly DateTimeOffset UtcNow = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Generate_Should_Return_Url_Safe_Token_And_Expected_Expirations()
        {
            var helper = CreateHelper();

            var result = helper.Generate(UtcNow);

            result.Value.Should().NotBeNullOrWhiteSpace();
            result.Value.Should().MatchRegex("^[A-Za-z0-9_-]+$");
            result.Verifier.Should().MatchRegex("^[A-F0-9]{64}$");
            result
                .IdleExpiresAt.Should()
                .Be(UtcNow.AddDays(AuthConstants.RefreshTokenIdleExpirationDays));
            result
                .AbsoluteExpiresAt.Should()
                .Be(UtcNow.AddDays(AuthConstants.RefreshTokenAbsoluteExpirationDays));
        }

        [Fact]
        public void Generate_Should_Create_Different_Tokens()
        {
            var helper = CreateHelper();

            var first = helper.Generate(UtcNow);
            var second = helper.Generate(UtcNow);

            first.Value.Should().NotBe(second.Value);
            first.Verifier.Should().NotBe(second.Verifier);
        }

        [Fact]
        public void ComputeVerifier_Should_Be_Deterministic()
        {
            var helper = CreateHelper();
            const string refreshToken = "refresh-token";

            var first = helper.ComputeVerifier(refreshToken);
            var second = helper.ComputeVerifier(refreshToken);

            first.Should().Be(second);
            first.Should().HaveLength(64);
            first.Should().NotBe(refreshToken);
        }

        [Fact]
        public void ComputeVerifier_Should_Change_When_Pepper_Changes()
        {
            const string refreshToken = "refresh-token";
            var first = CreateHelper(Pepper).ComputeVerifier(refreshToken);
            var second = CreateHelper("another-independent-pepper").ComputeVerifier(refreshToken);

            first.Should().NotBe(second);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Pepper_Is_Missing()
        {
            var configuration = new ConfigurationBuilder().Build();

            var action = () => new RefreshTokenHelper(configuration);

            action
                .Should()
                .Throw<InvalidOperationException>()
                .WithMessage($"*{Env.RefreshTokenPepperStringKey}*");
        }

        private static RefreshTokenHelper CreateHelper(string pepper = Pepper)
        {
            var values = new Dictionary<string, string>
            {
                [Env.RefreshTokenPepperStringKey] = pepper,
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

            return new RefreshTokenHelper(configuration);
        }
    }
}
