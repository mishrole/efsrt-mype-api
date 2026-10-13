using System;
using System.Collections.Generic;

namespace Mype.Application.FinancialMovements.Models
{
    public enum FinancialDashboardGranularity
    {
        Auto = 0,
        Day = 1,
        Week = 2,
        Month = 3,
        Year = 4,
    }

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
