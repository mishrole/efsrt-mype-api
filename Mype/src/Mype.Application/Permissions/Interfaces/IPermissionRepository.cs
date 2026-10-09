using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.Permissions.Interfaces
{
    public interface IPermissionRepository
    {
        Task<IReadOnlyCollection<string>>
            ListActiveCodesByRoleIdAsync(
                Guid roleId,
                CancellationToken cancellationToken
            );
    }
}