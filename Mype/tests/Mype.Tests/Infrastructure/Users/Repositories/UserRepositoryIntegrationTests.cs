using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Domain.Users;
using Mype.Infrastructure.Persistence;
using Mype.Infrastructure.Users.Repositories;
using Mype.Shared.Constants;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.Users.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class UserRepositoryIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public UserRepositoryIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task AddAsync_Should_Persist_User_And_Exists_Should_Return_True()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new UserRepository(context);
            var user = CreateUser();

            await repository.AddAsync(user, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);

            (
                await repository.ExistsByNormalizedEmailAsync(
                    user.NormalizedEmail,
                    CancellationToken.None
                )
            )
                .Should()
                .BeTrue();
            (await context.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id))
                .Email.Should()
                .Be(user.Email);
        }

        [Fact]
        public async Task GetByNormalizedEmailAsync_Should_Return_Detached_User()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new UserRepository(context);
            var user = CreateUser();
            await repository.AddAsync(user, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var result = await repository.GetByNormalizedEmailAsync(
                user.NormalizedEmail,
                CancellationToken.None
            );

            result.Should().NotBeNull();
            result.Id.Should().Be(user.Id);
            context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Detached_User()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new UserRepository(context);
            var user = CreateUser();
            await repository.AddAsync(user, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var result = await repository.GetByIdAsync(user.Id, CancellationToken.None);

            result.Should().NotBeNull();
            result.NormalizedEmail.Should().Be(user.NormalizedEmail);
            context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task SaveChangesAsync_Should_Translate_Real_Unique_Violation()
        {
            await using var factory = CreateFactory();
            await using var firstScope = factory.Services.CreateAsyncScope();
            var firstContext = firstScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var normalizedEmail = UniqueEmail().ToUpperInvariant();
            firstContext.Users.Add(CreateUser(normalizedEmail));
            await firstContext.SaveChangesAsync(CancellationToken.None);

            await using var secondScope = factory.Services.CreateAsyncScope();
            var secondContext = secondScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            secondContext.Users.Add(CreateUser(normalizedEmail));

            var action = () => secondContext.SaveChangesAsync(CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ApplicationErrorException>();
            exception.Which.Code.Should().Be(ErrorCodes.EmailAlreadyRegistered);
            exception.Which.ErrorType.Should().Be(ApplicationErrorType.Conflict);
        }

        private MypeWebApplicationFactory CreateFactory() => new(_database.ConnectionString);

        private static User CreateUser(string normalizedEmail = null)
        {
            var email = UniqueEmail();
            return User.Create(
                email,
                normalizedEmail ?? email.ToUpperInvariant(),
                "hash",
                "Integration User",
                DateTimeOffset.UtcNow
            );
        }

        private static string UniqueEmail() => $"repository-{Guid.NewGuid():N}@example.com";
    }
}
