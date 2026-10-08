using Mype.Domain.BusinessMemberships;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.BusinessMemberships.Interfaces
{
    public interface IBusinessMembershipRepository
    {
        Task AddAsync(
            BusinessMembership membership,
            CancellationToken cancellationToken
        );
    }
}