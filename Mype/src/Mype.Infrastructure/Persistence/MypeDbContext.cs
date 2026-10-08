using Microsoft.EntityFrameworkCore;
using Mype.Application.Common.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.BusinessRoles;
using Mype.Domain.Categories;
using Mype.Domain.Currencies;
using Mype.Domain.Users;
using Mype.Infrastructure.Persistence.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Infrastructure.Persistence
{
    public class MypeDbContext : DbContext, IUnitOfWork
    {
        private readonly IPersistenceExceptionTranslator _exceptionTranslator;

        public MypeDbContext(
            DbContextOptions<MypeDbContext> options,
            IPersistenceExceptionTranslator exceptionTranslator
        ) : base(options) {
            _exceptionTranslator = exceptionTranslator;
        }

        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                var translatedException = _exceptionTranslator.Translate(exception);

                if (ReferenceEquals(translatedException, exception))
                {
                    throw;
                }

                throw translatedException;
            }
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Currency> Currencies => Set<Currency>();
        public DbSet<BusinessRole> BusinessRoles => Set<BusinessRole>();
        public DbSet<Business> Businesses => Set<Business>();
        public DbSet<BusinessMembership> BusinessMemberships => Set<BusinessMembership>();
        public DbSet<Category> Categories => Set<Category>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MypeDbContext).Assembly);
        }
    }
}
