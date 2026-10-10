using System.Threading;
using System.Threading.Tasks;
using Mype.Domain.Businesses;

namespace Mype.Application.Businesses.Interfaces
{
    public interface IBusinessRepository
    {
        Task AddAsync(Business business, CancellationToken cancellationToken);

        Task<bool> ExistsByRucAsync(string ruc, CancellationToken cancellationToken);
    }
}
