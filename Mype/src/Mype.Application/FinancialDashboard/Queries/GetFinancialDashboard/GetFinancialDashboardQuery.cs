using System;
using MediatR;
using Mype.Application.Common;
using Mype.Application.FinancialDashboard.Enums;

namespace Mype.Application.FinancialDashboard.Queries.GetFinancialDashboard
{
    public sealed class GetFinancialDashboardQuery : IRequest<Result<FinancialDashboardResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid CurrentUserId { get; set; }
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public FinancialDashboardGranularity Granularity { get; set; } =
            FinancialDashboardGranularity.Auto;
    }
}
