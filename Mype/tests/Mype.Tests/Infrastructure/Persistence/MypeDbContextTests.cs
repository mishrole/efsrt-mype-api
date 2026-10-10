using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Mype.Infrastructure.Persistence;
using Mype.Infrastructure.Persistence.Exceptions;

namespace Mype.Tests.Infrastructure.Persistence
{
    public class MypeDbContextTests
    {
        [Fact]
        public void DbSets_Should_Be_Available()
        {
            using var context = CreateContext();

            context.Users.Should().NotBeNull();
            context.Currencies.Should().NotBeNull();
            context.BusinessRoles.Should().NotBeNull();
            context.Businesses.Should().NotBeNull();
            context.BusinessMemberships.Should().NotBeNull();
            context.Categories.Should().NotBeNull();
            context.Permissions.Should().NotBeNull();
            context.BusinessRolePermissions.Should().NotBeNull();
            context.Products.Should().NotBeNull();
            context.FinancialMovements.Should().NotBeNull();
            context.FinancialMovementItems.Should().NotBeNull();
        }

        [Fact]
        public async Task SaveChangesAsync_Should_Return_Zero_When_No_Changes_Exist()
        {
            using var context = CreateContext();

            var result = await context.SaveChangesAsync(CancellationToken.None);

            result.Should().Be(0);
        }

        private static MypeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<MypeDbContext>()
                .UseNpgsql("Host=localhost;Database=mype_tests;Username=test;Password=test")
                .Options;

            var translatorMock = new Mock<IPersistenceExceptionTranslator>();

            return new MypeDbContext(options, translatorMock.Object);
        }
    }
}
