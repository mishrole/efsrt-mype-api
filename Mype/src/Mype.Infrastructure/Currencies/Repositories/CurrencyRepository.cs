using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mype.Application.Currencies.Interfaces;
using Mype.Domain.Currencies;
using Mype.Infrastructure.Persistence;

namespace Mype.Infrastructure.Currencies.Repositories
{
    public class CurrencyRepository : ICurrencyRepository
    {
        private readonly MypeDbContext _dbContext;

        public CurrencyRepository(MypeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Currency> GetActiveByCodeAsync(string code, CancellationToken cancellationToken)
        {
            return _dbContext
                .Currencies.AsNoTracking()
                .SingleOrDefaultAsync(
                    currency => currency.Code == code && currency.IsActive,
                    cancellationToken
                );
        }
    }
}
