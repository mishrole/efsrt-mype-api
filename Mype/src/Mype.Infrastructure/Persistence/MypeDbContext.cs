using Microsoft.EntityFrameworkCore;
using Mype.Application.Common.Interfaces;
using Mype.Domain.Users;

namespace Mype.Infrastructure.Persistence
{
    public class MypeDbContext : DbContext, IUnitOfWork
    {
        public MypeDbContext(DbContextOptions<MypeDbContext> options) : base(options) {}

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MypeDbContext).Assembly);
        }
    }
}
