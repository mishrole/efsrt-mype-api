using Microsoft.EntityFrameworkCore;
using Mype.Application.Permissions.Interfaces;
using Mype.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Infrastructure.Permissions.Repositories
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly MypeDbContext _dbContext;

        public PermissionRepository(
            MypeDbContext dbContext
        )
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<string>>
            ListActiveCodesByRoleIdAsync(
                Guid roleId,
                CancellationToken cancellationToken
            )
        {
            return await (
                from relation in
                    _dbContext.BusinessRolePermissions
                        .AsNoTracking()
                join permission in
                    _dbContext.Permissions.AsNoTracking()
                    on relation.PermissionId equals
                    permission.Id
                where
                    relation.BusinessRoleId == roleId &&
                    permission.IsActive
                orderby permission.Code
                select permission.Code
            ).ToArrayAsync(cancellationToken);
        }
    }
}