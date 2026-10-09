using Microsoft.EntityFrameworkCore;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Infrastructure.BusinessMemberships.Repositories
{
    public class BusinessMembershipRepository : IBusinessMembershipRepository
    {
        private readonly MypeDbContext _dbContext;

        public BusinessMembershipRepository(
            MypeDbContext dbContext
        )
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(
            BusinessMembership membership,
            CancellationToken cancellationToken
        )
        {
            await _dbContext.BusinessMemberships.AddAsync(
                membership,
                cancellationToken
            );
        }

        public async Task<
            IReadOnlyCollection<
                BusinessSummaryProjection
            >
        > ListActiveByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken
        )
        {
            return await (
                from membership in
                    _dbContext.BusinessMemberships
                        .AsNoTracking()
                join business in
                    _dbContext.Businesses.AsNoTracking()
                    on membership.BusinessId equals
                    business.Id
                join role in
                    _dbContext.BusinessRoles.AsNoTracking()
                    on membership.RoleId equals role.Id
                join currency in
                    _dbContext.Currencies.AsNoTracking()
                    on business.CurrencyId equals currency.Id
                where
                    membership.UserId == userId &&
                    membership.Status ==
                        BusinessMembershipStatus.Active &&
                    business.Status ==
                        BusinessStatus.Active &&
                    role.IsActive &&
                    currency.IsActive
                orderby business.DisplayName
                select new BusinessSummaryProjection(
                    business.Id,
                    business.DisplayName,
                    currency.Code,
                    business.Status,
                    membership.Id,
                    role.Code
                )
            ).ToArrayAsync(cancellationToken);
        }

        public Task<BusinessContextProjection>
            GetContextByBusinessAndUserAsync(
                Guid businessId,
                Guid userId,
                CancellationToken cancellationToken
            )
        {
            return (
                from membership in
                    _dbContext.BusinessMemberships
                        .AsNoTracking()
                join business in
                    _dbContext.Businesses.AsNoTracking()
                    on membership.BusinessId equals
                    business.Id
                join role in
                    _dbContext.BusinessRoles.AsNoTracking()
                    on membership.RoleId equals role.Id
                join currency in
                    _dbContext.Currencies.AsNoTracking()
                    on business.CurrencyId equals currency.Id
                where
                    membership.BusinessId == businessId &&
                    membership.UserId == userId
                select new BusinessContextProjection(
                    business.Id,
                    business.DisplayName,
                    currency.Code,
                    business.Status,
                    membership.Id,
                    membership.Status,
                    role.Id,
                    role.Code,
                    role.IsActive
                )
            ).SingleOrDefaultAsync(cancellationToken);
        }
    }
}