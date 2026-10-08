using Mype.Domain.BusinessRoles;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.BusinessRoles.Interfaces
{
    public interface IBusinessRoleRepository
    {
        Task<BusinessRole> GetActiveByCodeAsync(
            string code,
            CancellationToken cancellationToken
        );
    }
}