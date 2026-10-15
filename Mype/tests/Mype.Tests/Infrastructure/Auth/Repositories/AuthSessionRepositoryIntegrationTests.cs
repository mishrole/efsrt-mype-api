using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Application.Common.Exceptions;
using Mype.Domain.AuthSessions;
using Mype.Infrastructure.Auth.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Shared.Constants;
using Mype.Tests.Infrastructure.Common;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.Auth.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class AuthSessionRepositoryIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public AuthSessionRepositoryIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task AddAsync_Should_Persist_AuthSession()
        {
            await using var factory = CreateFactory();

            await using var scope = factory.Services.CreateAsyncScope();

            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();

            var seed = await RepositoryTestData.SeedBusinessAsync(context);

            var repository = new AuthSessionRepository(context);

            var verifier = UniqueVerifier();

            var session = CreateSession(seed.User.Id, verifier);

            await repository.AddAsync(session, CancellationToken.None);

            await context.SaveChangesAsync(CancellationToken.None);

            context.ChangeTracker.Clear();

            var persisted = await context
                .AuthSessions.AsNoTracking()
                .SingleAsync(current => current.Id == session.Id);

            persisted.RefreshTokenVerifier.Should().Be(verifier);

            persisted.FamilyId.Should().Be(session.FamilyId);

            persisted.UserId.Should().Be(seed.User.Id);

            persisted.IdleExpiresAt.Should().Be(session.IdleExpiresAt);

            persisted.AbsoluteExpiresAt.Should().Be(session.AbsoluteExpiresAt);

            persisted.RevokedAt.Should().BeNull();
            persisted.RevocationReason.Should().BeNull();
            persisted.ReplacedBySessionId.Should().BeNull();
            persisted.Version.Should().BeGreaterThan(0u);
        }

        [Fact]
        public async Task GetByVerifierForUpdateAsync_Should_Return_Tracked_Session_And_Persist_Revocation()
        {
            await using var factory = CreateFactory();

            var verifier = UniqueVerifier();

            var sessionId = await SeedSessionAsync(factory, verifier);

            await using var scope = factory.Services.CreateAsyncScope();

            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();

            var repository = new AuthSessionRepository(context);

            var session = await repository.GetByVerifierForUpdateAsync(
                verifier,
                CancellationToken.None
            );

            session.Should().NotBeNull();

            context.Entry(session!).State.Should().Be(EntityState.Unchanged);

            var revokedAt = RepositoryTestData.UtcNow.AddHours(1);

            session.Revoke(AuthSessionRevocationReason.Logout, revokedAt);

            await context.SaveChangesAsync(CancellationToken.None);

            context.ChangeTracker.Clear();

            var persisted = await context
                .AuthSessions.AsNoTracking()
                .SingleAsync(current => current.Id == sessionId);

            persisted.RevokedAt.Should().Be(revokedAt);

            persisted.RevocationReason.Should().Be(AuthSessionRevocationReason.Logout);
        }

        [Fact]
        public async Task IsActiveAsync_Should_Filter_By_Session_User_Revocation_And_Expiration()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var repository = new AuthSessionRepository(context);
            var active = CreateSession(seed.User.Id, UniqueVerifier());
            var revoked = CreateSession(seed.User.Id, UniqueVerifier());
            var idleExpired = AuthSession.Create(
                seed.User.Id,
                UniqueVerifier(),
                RepositoryTestData.UtcNow.AddDays(-8),
                RepositoryTestData.UtcNow.AddMinutes(-1),
                RepositoryTestData.UtcNow.AddDays(22)
            );
            var absoluteExpired = AuthSession.Create(
                seed.User.Id,
                UniqueVerifier(),
                RepositoryTestData.UtcNow.AddDays(-31),
                RepositoryTestData.UtcNow.AddDays(1),
                RepositoryTestData.UtcNow.AddMinutes(-1)
            );
            revoked.Revoke(
                AuthSessionRevocationReason.Logout,
                RepositoryTestData.UtcNow.AddMinutes(-1)
            );
            context.AuthSessions.AddRange(active, revoked, idleExpired, absoluteExpired);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            (
                await repository.IsActiveAsync(
                    active.Id,
                    seed.User.Id,
                    RepositoryTestData.UtcNow,
                    CancellationToken.None
                )
            )
                .Should()
                .BeTrue();
            (
                await repository.IsActiveAsync(
                    active.Id,
                    Guid.NewGuid(),
                    RepositoryTestData.UtcNow,
                    CancellationToken.None
                )
            )
                .Should()
                .BeFalse();
            (
                await repository.IsActiveAsync(
                    revoked.Id,
                    seed.User.Id,
                    RepositoryTestData.UtcNow,
                    CancellationToken.None
                )
            )
                .Should()
                .BeFalse();
            (
                await repository.IsActiveAsync(
                    idleExpired.Id,
                    seed.User.Id,
                    RepositoryTestData.UtcNow,
                    CancellationToken.None
                )
            )
                .Should()
                .BeFalse();
            (
                await repository.IsActiveAsync(
                    absoluteExpired.Id,
                    seed.User.Id,
                    RepositoryTestData.UtcNow,
                    CancellationToken.None
                )
            )
                .Should()
                .BeFalse();
            context.ChangeTracker.Entries<AuthSession>().Should().BeEmpty();
        }

        [Fact]
        public async Task ListActiveByFamilyIdAsync_Should_Return_Only_Active_Replacement_From_Family()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var repository = new AuthSessionRepository(context);
            var original = CreateSession(seed.User.Id, UniqueVerifier());
            var replacement = original.Rotate(
                UniqueVerifier(),
                RepositoryTestData.UtcNow.AddMinutes(1),
                RepositoryTestData.UtcNow.AddDays(7)
            );
            var unrelated = CreateSession(seed.User.Id, UniqueVerifier());
            context.AuthSessions.AddRange(original, replacement, unrelated);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var result = await repository.ListActiveByFamilyIdAsync(
                original.FamilyId,
                RepositoryTestData.UtcNow.AddMinutes(2),
                CancellationToken.None
            );

            result.Should().ContainSingle();
            result.Single().Id.Should().Be(replacement.Id);
            result.Should().NotContain(current => current.Id == original.Id);
            result.Should().NotContain(current => current.Id == unrelated.Id);
            context.Entry(result.Single()).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task ListActiveByUserIdAsync_Should_Return_Only_Active_User_Sessions()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var firstSeed = await RepositoryTestData.SeedBusinessAsync(context);
            var secondSeed = await RepositoryTestData.SeedBusinessAsync(context);
            var repository = new AuthSessionRepository(context);
            var firstActive = CreateSession(firstSeed.User.Id, UniqueVerifier());
            var secondActive = CreateSession(firstSeed.User.Id, UniqueVerifier());
            var revoked = CreateSession(firstSeed.User.Id, UniqueVerifier());
            var foreign = CreateSession(secondSeed.User.Id, UniqueVerifier());
            revoked.Revoke(
                AuthSessionRevocationReason.Logout,
                RepositoryTestData.UtcNow.AddMinutes(-1)
            );
            context.AuthSessions.AddRange(firstActive, secondActive, revoked, foreign);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var result = await repository.ListActiveByUserIdAsync(
                firstSeed.User.Id,
                RepositoryTestData.UtcNow,
                CancellationToken.None
            );

            result
                .Select(current => current.Id)
                .Should()
                .BeEquivalentTo(new[] { firstActive.Id, secondActive.Id });
            result.Should().NotContain(current => current.Id == revoked.Id);
            result.Should().NotContain(current => current.Id == foreign.Id);
        }

        [Fact]
        public async Task SaveChangesAsync_Should_Reject_Duplicate_RefreshTokenVerifier()
        {
            await using var factory = CreateFactory();

            await using var scope = factory.Services.CreateAsyncScope();

            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();

            var seed = await RepositoryTestData.SeedBusinessAsync(context);

            var repository = new AuthSessionRepository(context);

            var duplicatedVerifier = UniqueVerifier();

            await repository.AddAsync(
                CreateSession(seed.User.Id, duplicatedVerifier),
                CancellationToken.None
            );

            await repository.AddAsync(
                CreateSession(seed.User.Id, duplicatedVerifier),
                CancellationToken.None
            );

            var action = () => context.SaveChangesAsync(CancellationToken.None);

            await action.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task Rotation_Should_Persist_ReplacedBySession_Relationship()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var original = CreateSession(seed.User.Id, UniqueVerifier());
            var replacement = original.Rotate(
                UniqueVerifier(),
                RepositoryTestData.UtcNow.AddMinutes(1),
                RepositoryTestData.UtcNow.AddDays(7)
            );
            context.AuthSessions.AddRange(original, replacement);

            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var persistedOriginal = await context
                .AuthSessions.AsNoTracking()
                .SingleAsync(current => current.Id == original.Id);
            var persistedReplacement = await context
                .AuthSessions.AsNoTracking()
                .SingleAsync(current => current.Id == replacement.Id);

            persistedOriginal.RevocationReason.Should().Be(AuthSessionRevocationReason.Rotated);
            persistedOriginal.ReplacedBySessionId.Should().Be(replacement.Id);
            persistedReplacement.FamilyId.Should().Be(original.FamilyId);
        }

        [Fact]
        public async Task SaveChangesAsync_Should_Reject_Stale_AuthSession_Version()
        {
            await using var factory = CreateFactory();
            var sessionId = await SeedSessionAsync(factory, UniqueVerifier());
            await using var firstScope = factory.Services.CreateAsyncScope();
            await using var staleScope = factory.Services.CreateAsyncScope();
            var firstContext = firstScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var staleContext = staleScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var first = await firstContext.AuthSessions.SingleAsync(current =>
                current.Id == sessionId
            );
            var stale = await staleContext.AuthSessions.SingleAsync(current =>
                current.Id == sessionId
            );

            first.Revoke(
                AuthSessionRevocationReason.Logout,
                RepositoryTestData.UtcNow.AddMinutes(1)
            );
            await firstContext.SaveChangesAsync(CancellationToken.None);

            stale.Revoke(
                AuthSessionRevocationReason.AccountDeactivated,
                RepositoryTestData.UtcNow.AddMinutes(2)
            );
            var action = () => staleContext.SaveChangesAsync(CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ApplicationErrorException>();
            exception.Which.Code.Should().Be(ErrorCodes.ConcurrencyConflict);

            staleContext.ChangeTracker.Clear();
            var persisted = await staleContext
                .AuthSessions.AsNoTracking()
                .SingleAsync(current => current.Id == sessionId);
            persisted.RevocationReason.Should().Be(AuthSessionRevocationReason.Logout);
        }

        private MypeWebApplicationFactory CreateFactory() => new(_database.ConnectionString);

        private static AuthSession CreateSession(Guid userId, string verifier)
        {
            return AuthSession.Create(
                userId,
                verifier,
                RepositoryTestData.UtcNow,
                RepositoryTestData.UtcNow.AddDays(7),
                RepositoryTestData.UtcNow.AddDays(30)
            );
        }

        private static async Task<Guid> SeedSessionAsync(
            MypeWebApplicationFactory factory,
            string verifier
        )
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var session = CreateSession(seed.User.Id, verifier);
            context.AuthSessions.Add(session);
            await context.SaveChangesAsync(CancellationToken.None);
            return session.Id;
        }

        private static string UniqueVerifier()
        {
            return Guid.NewGuid().ToString("N").ToUpperInvariant()
                + Guid.NewGuid().ToString("N").ToUpperInvariant();
        }
    }
}
