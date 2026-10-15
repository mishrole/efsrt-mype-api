using System;
using FluentAssertions;
using Mype.Domain.AuthSessions;
using Mype.Shared.Constants;

namespace Mype.Tests.Domain.AuthSessions
{
    public class AuthSessionTests
    {
        private const string Verifier = "VERIFIER";
        private const string ReplacementVerifier = "REPLACEMENT_VERIFIER";

        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly DateTimeOffset UtcNow = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Create_Should_Create_Active_Session_With_Independent_Family()
        {
            var session = CreateSession();

            session.Id.Should().NotBeEmpty();
            session.FamilyId.Should().NotBeEmpty();
            session.UserId.Should().Be(UserId);
            session.RefreshTokenVerifier.Should().Be(Verifier);
            session.CreatedAt.Should().Be(UtcNow);
            session.UpdatedAt.Should().Be(UtcNow);
            session.IdleExpiresAt.Should().Be(UtcNow.AddDays(7));
            session.AbsoluteExpiresAt.Should().Be(UtcNow.AddDays(30));
            session.RevokedAt.Should().BeNull();
            session.RevocationReason.Should().BeNull();
            session.ReplacedBySessionId.Should().BeNull();
            session.IsActive(UtcNow).Should().BeTrue();
        }

        [Fact]
        public void Rotate_Should_Revoke_Current_Session_And_Create_Replacement_In_Same_Family()
        {
            var session = CreateSession();
            var rotatedAt = UtcNow.AddDays(1);

            var replacement = session.Rotate(ReplacementVerifier, rotatedAt, rotatedAt.AddDays(7));

            session.RevokedAt.Should().Be(rotatedAt);
            session.RevocationReason.Should().Be(AuthSessionRevocationReason.Rotated);
            session.ReplacedBySessionId.Should().Be(replacement.Id);
            session.IsRotated().Should().BeTrue();
            session.IsActive(rotatedAt).Should().BeFalse();

            replacement.Id.Should().NotBe(session.Id);
            replacement.FamilyId.Should().Be(session.FamilyId);
            replacement.UserId.Should().Be(session.UserId);
            replacement.RefreshTokenVerifier.Should().Be(ReplacementVerifier);
            replacement.AbsoluteExpiresAt.Should().Be(session.AbsoluteExpiresAt);
            replacement.IsActive(rotatedAt).Should().BeTrue();
        }

        [Fact]
        public void Rotate_Should_Not_Extend_Idle_Expiration_Beyond_Absolute_Expiration()
        {
            var absoluteExpiresAt = UtcNow.AddDays(30);

            var session = AuthSession.Create(
                UserId,
                Verifier,
                UtcNow,
                absoluteExpiresAt,
                absoluteExpiresAt
            );

            var rotatedAt = UtcNow.AddDays(29);

            var replacement = session.Rotate(
                ReplacementVerifier,
                rotatedAt,
                rotatedAt.AddDays(AuthConstants.RefreshTokenIdleExpirationDays)
            );

            replacement.IdleExpiresAt.Should().Be(absoluteExpiresAt);

            replacement.AbsoluteExpiresAt.Should().Be(absoluteExpiresAt);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(30)]
        [InlineData(31)]
        public void IsActive_Should_Return_False_When_Session_Has_Expired(int elapsedDays)
        {
            var session = CreateSession();

            session.IsActive(UtcNow.AddDays(elapsedDays)).Should().BeFalse();
        }

        [Fact]
        public void Revoke_Should_Be_Idempotent()
        {
            var session = CreateSession();
            var firstRevocation = UtcNow.AddHours(1);

            session.Revoke(AuthSessionRevocationReason.Logout, firstRevocation);
            session.Revoke(
                AuthSessionRevocationReason.AccountDeactivated,
                firstRevocation.AddHours(1)
            );

            session.RevokedAt.Should().Be(firstRevocation);
            session.RevocationReason.Should().Be(AuthSessionRevocationReason.Logout);
        }

        [Fact]
        public void Rotate_Should_Throw_When_Session_Is_Not_Active()
        {
            var session = CreateSession();
            session.Revoke(AuthSessionRevocationReason.Logout, UtcNow.AddMinutes(1));

            var action = () =>
                session.Rotate(ReplacementVerifier, UtcNow.AddMinutes(2), UtcNow.AddDays(7));

            action.Should().Throw<InvalidOperationException>();
        }

        private static AuthSession CreateSession()
        {
            return AuthSession.Create(
                UserId,
                Verifier,
                UtcNow,
                UtcNow.AddDays(7),
                UtcNow.AddDays(30)
            );
        }
    }
}
