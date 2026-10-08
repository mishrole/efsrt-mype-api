using Mype.Domain.Currencies;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.Currencies.Interfaces
{
    public interface ICurrencyRepository
    {
        Task<Currency> GetActiveByCodeAsync(
            string code,
            CancellationToken cancellationToken
        );
    }
}