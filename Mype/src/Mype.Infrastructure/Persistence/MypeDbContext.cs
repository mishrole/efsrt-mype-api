using Microsoft.EntityFrameworkCore;

namespace Mype.Infrastructure.Persistence
{
    public class MypeDbContext : DbContext
    {
        public MypeDbContext(DbContextOptions<MypeDbContext> options) : base(options) {}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MypeDbContext).Assembly);
        }
    }
}
