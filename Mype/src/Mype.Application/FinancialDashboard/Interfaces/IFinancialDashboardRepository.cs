using System;
using System.Threading;
using System.Threading.Tasks;
using Mype.Application.FinancialDashboard.Models;

namespace Mype.Application.FinancialDashboard.Interfaces
{
    public interface IFinancialDashboardRepository
    {
        Task<FinancialDashboardProjection> GetAsync(
            Guid businessId,
            DateOnly dateTo,
            CancellationToken cancellationToken
        );
    }
}
