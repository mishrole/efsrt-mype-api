using System;
using System.Collections.Generic;
using Mype.Application.FinancialDashboard.Enums;

namespace Mype.Application.FinancialDashboard.Queries.GetFinancialDashboard
{
    public sealed record FinancialDashboardResult(
        Guid BusinessId,
        string CurrencyCode,
        DateOnly DateFrom,
        DateOnly DateTo,
        FinancialDashboardGranularity Granularity,
        decimal OpeningBalance,
        decimal CashInflow,
        decimal CashOutflow,
        decimal NetCashFlow,
        decimal ClosingBalance,
        decimal EstimatedCost,
        decimal EstimatedGrossMargin,
        int ConfirmedSaleCount,
        int ConfirmedExpenseCount,
        decimal AverageSaleAmount,
        IReadOnlyCollection<FinancialTrendPointResult> Trend,
        IReadOnlyCollection<CategoryAmountResult> SaleByCategory,
        IReadOnlyCollection<CategoryAmountResult> ExpenseByCategory
    );

    public sealed record FinancialTrendPointResult(
        DateOnly DateFrom,
        DateOnly DateTo,
        decimal CashInflow,
        decimal CashOutflow,
        decimal NetCashFlow,
        decimal ClosingBalance
    );

    public sealed record CategoryAmountResult(
        Guid CategoryId,
        string CategoryName,
        bool CategoryIsActive,
        decimal Amount,
        decimal Percentage
    );
}
