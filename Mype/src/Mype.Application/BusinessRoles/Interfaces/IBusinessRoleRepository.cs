using System.Threading;
using System.Threading.Tasks;
using Mype.Domain.BusinessRoles;

namespace Mype.Application.BusinessRoles.Interfaces
{
    public interface IBusinessRoleRepository
    {
        Task<BusinessRole> GetActiveByCodeAsync(string code, CancellationToken cancellationToken);
    }
}
