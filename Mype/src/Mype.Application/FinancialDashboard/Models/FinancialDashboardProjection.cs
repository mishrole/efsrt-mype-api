using System;
using System.Collections.Generic;

namespace Mype.Application.FinancialDashboard.Models
{
    public sealed record FinancialDashboardPeriod(DateOnly DateFrom, DateOnly DateTo);

    public sealed record FinancialDashboardMovementProjection(
        DateOnly MovementDate,
        int Type,
        decimal TotalAmount,
        decimal EstimatedCost
    );

    public sealed record FinancialDashboardCategoryProjection(
        Guid CategoryId,
        string CategoryName,
        bool CategoryIsActive,
        int Type,
        DateOnly MovementDate,
        decimal Amount
    );

    public sealed record FinancialDashboardProjection(
        decimal OpeningBalanceAmount,
        DateOnly OpeningBalanceDate,
        IReadOnlyCollection<FinancialDashboardMovementProjection> Movements,
        IReadOnlyCollection<FinancialDashboardCategoryProjection> Categories
    );
}
