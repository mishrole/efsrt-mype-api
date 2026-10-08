using Mype.Domain.Businesses;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.Businesses.Interfaces
{
    public interface IBusinessRepository
    {
        Task AddAsync(
            Business business,
            CancellationToken cancellationToken
        );
    }
}