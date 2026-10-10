using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Domain.BusinessMemberships;
using Mype.Infrastructure.BusinessMemberships.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Tests.Infrastructure.Common;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.BusinessMemberships.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class BusinessMembershipRepositoryIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public BusinessMembershipRepositoryIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task AddAsync_Context_And_List_Should_Project_Active_Membership()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var role = await context.BusinessRoles.AsNoTracking().FirstAsync(item => item.IsActive);
            var repository = new BusinessMembershipRepository(context);
            var membership = BusinessMembership.CreateOwner(
                seed.Business.Id,
                seed.User.Id,
                role.Id,
                RepositoryTestData.UtcNow
            );

            await repository.AddAsync(membership, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var businessContext = await repository.GetContextByBusinessAndUserAsync(
                seed.Business.Id,
                seed.User.Id,
                CancellationToken.None
            );
            var summaries = await repository.ListActiveByUserIdAsync(
                seed.User.Id,
                CancellationToken.None
            );

            businessContext.Should().NotBeNull();
            businessContext.BusinessId.Should().Be(seed.Business.Id);
            businessContext.MembershipId.Should().Be(membership.Id);
            businessContext.RoleId.Should().Be(role.Id);
            businessContext.RoleCode.Should().Be(role.Code);
            businessContext.CurrencyCode.Should().Be("PEN");
            summaries
                .Should()
                .ContainSingle(item =>
                    item.BusinessId == seed.Business.Id
                    && item.MembershipId == membership.Id
                    && item.RoleCode == role.Code
                );
            context.ChangeTracker.Entries<BusinessMembership>().Should().BeEmpty();
        }

        [Fact]
        public async Task ListActiveByUserIdAsync_Should_Exclude_Inactive_Membership()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var role = await context.BusinessRoles.AsNoTracking().FirstAsync(item => item.IsActive);
            var repository = new BusinessMembershipRepository(context);
            var membership = BusinessMembership.CreateOwner(
                seed.Business.Id,
                seed.User.Id,
                role.Id,
                RepositoryTestData.UtcNow
            );
            membership.Deactivate(seed.User.Id, RepositoryTestData.UtcNow.AddMinutes(1));
            await repository.AddAsync(membership, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);

            (await repository.ListActiveByUserIdAsync(seed.User.Id, CancellationToken.None))
                .Should()
                .BeEmpty();
        }

        [Fact]
        public async Task GetContextByBusinessAndUserAsync_Should_Isolate_User_And_Business()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var role = await context.BusinessRoles.AsNoTracking().FirstAsync(item => item.IsActive);
            var repository = new BusinessMembershipRepository(context);
            await repository.AddAsync(
                BusinessMembership.CreateOwner(
                    seed.Business.Id,
                    seed.User.Id,
                    role.Id,
                    RepositoryTestData.UtcNow
                ),
                CancellationToken.None
            );
            await context.SaveChangesAsync(CancellationToken.None);

            (
                await repository.GetContextByBusinessAndUserAsync(
                    seed.Business.Id,
                    System.Guid.NewGuid(),
                    CancellationToken.None
                )
            )
                .Should()
                .BeNull();
            (
                await repository.GetContextByBusinessAndUserAsync(
                    System.Guid.NewGuid(),
                    seed.User.Id,
                    CancellationToken.None
                )
            )
                .Should()
                .BeNull();
        }
    }
}
