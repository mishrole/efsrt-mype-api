using System.Threading;
using System.Threading.Tasks;
using Mype.Domain.Currencies;

namespace Mype.Application.Currencies.Interfaces
{
    public interface ICurrencyRepository
    {
        Task<Currency> GetActiveByCodeAsync(string code, CancellationToken cancellationToken);
    }
}
