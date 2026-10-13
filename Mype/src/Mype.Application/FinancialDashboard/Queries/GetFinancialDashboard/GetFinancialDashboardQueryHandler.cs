using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialDashboard.Enums;
using Mype.Application.FinancialDashboard.Interfaces;
using Mype.Application.FinancialDashboard.Models;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;

namespace Mype.Application.FinancialDashboard.Queries.GetFinancialDashboard
{
    public sealed class GetFinancialDashboardQueryHandler
        : IRequestHandler<GetFinancialDashboardQuery, Result<FinancialDashboardResult>>
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialDashboardRepository _dashboard;
        private readonly IClock _clock;

        public GetFinancialDashboardQueryHandler(
            IBusinessMembershipRepository memberships,
            IPermissionRepository permissions,
            IFinancialDashboardRepository dashboard,
            IClock clock
        )
        {
            _memberships = memberships;
            _permissions = permissions;
            _dashboard = dashboard;
            _clock = clock;
        }

        public async Task<Result<FinancialDashboardResult>> Handle(
            GetFinancialDashboardQuery request,
            CancellationToken cancellationToken
        )
        {
            var context = await _memberships.GetContextByBusinessAndUserAsync(
                request.BusinessId,
                request.CurrentUserId,
                cancellationToken
            );
            if (
                context == null
                || context.MembershipStatus != BusinessMembershipStatus.Active
                || !context.RoleIsActive
            )
                return Result<FinancialDashboardResult>.Failure(
                    GetFinancialDashboardErrors.BusinessAccessForbidden
                );

            var permissions = await _permissions.ListActiveCodesByRoleIdAsync(
                context.RoleId,
                cancellationToken
            );
            if (!permissions.Contains(SystemPermissions.DashboardRead.Code))
                return Result<FinancialDashboardResult>.Failure(
                    GetFinancialDashboardErrors.DashboardAccessForbidden
                );
            var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
            var dateTo = request.DateTo ?? today;
            var dateFrom = request.DateFrom ?? dateTo.AddDays(-29);
            var granularity = ResolveGranularity(request.Granularity, dateFrom, dateTo);
            try
            {
                var source = await _dashboard.GetAsync(
                    request.BusinessId,
                    dateTo,
                    cancellationToken
                );
                if (source == null)
                    return Result<FinancialDashboardResult>.Failure(
                        GetFinancialDashboardErrors.OpeningBalanceNotFound
                    );
                var before = source.Movements.Where(m => m.MovementDate < dateFrom);
                var period = source
                    .Movements.Where(m => m.MovementDate >= dateFrom && m.MovementDate <= dateTo)
                    .ToArray();
                var opening = source.OpeningBalanceAmount + before.Sum(SignedAmount);
                var inflow = period.Where(IsSale).Sum(m => m.TotalAmount);
                var outflow = period.Where(IsExpense).Sum(m => m.TotalAmount);
                var net = inflow - outflow;
                var cost = period.Where(IsSale).Sum(m => m.EstimatedCost);
                var sales = period.Count(IsSale);
                var expenses = period.Count(IsExpense);
                var trend = BuildTrend(period, dateFrom, dateTo, granularity, opening);
                var categories = source
                    .Categories.Where(c =>
                        c.MovementDate >= dateFrom && c.MovementDate <= dateTo && c.Amount != 0m
                    )
                    .GroupBy(c => new
                    {
                        c.CategoryId,
                        c.CategoryName,
                        c.CategoryIsActive,
                        c.Type,
                    })
                    .Select(g => new FinancialDashboardCategoryProjection(
                        g.Key.CategoryId,
                        g.Key.CategoryName,
                        g.Key.CategoryIsActive,
                        g.Key.Type,
                        dateFrom,
                        g.Sum(x => x.Amount)
                    ))
                    .ToArray();
                return Result<FinancialDashboardResult>.Success(
                    new(
                        request.BusinessId,
                        context.CurrencyCode,
                        dateFrom,
                        dateTo,
                        granularity,
                        opening,
                        inflow,
                        outflow,
                        net,
                        opening + net,
                        cost,
                        inflow - cost,
                        sales,
                        expenses,
                        sales == 0
                            ? 0m
                            : decimal.Round(inflow / sales, 2, MidpointRounding.AwayFromZero),
                        trend,
                        MapCategories(categories, 1),
                        MapCategories(categories, 2)
                    )
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return Result<FinancialDashboardResult>.Failure(
                    GetFinancialDashboardErrors.QueryFailed
                );
            }
        }

        private static bool IsSale(FinancialDashboardMovementProjection m) =>
            m.Type == (int)FinancialMovementType.Sale;

        private static bool IsExpense(FinancialDashboardMovementProjection m) =>
            m.Type == (int)FinancialMovementType.Expense;

        private static decimal SignedAmount(FinancialDashboardMovementProjection m) =>
            IsSale(m) ? m.TotalAmount
            : IsExpense(m) ? -m.TotalAmount
            : 0m;

        private static FinancialDashboardGranularity ResolveGranularity(
            FinancialDashboardGranularity value,
            DateOnly from,
            DateOnly to
        )
        {
            if (value != FinancialDashboardGranularity.Auto)
                return value;
            var days = to.DayNumber - from.DayNumber + 1;
            return days <= 31 ? FinancialDashboardGranularity.Day
                : days <= 180 ? FinancialDashboardGranularity.Week
                : days <= 1095 ? FinancialDashboardGranularity.Month
                : FinancialDashboardGranularity.Year;
        }

        private static IReadOnlyCollection<FinancialTrendPointResult> BuildTrend(
            IReadOnlyCollection<FinancialDashboardMovementProjection> movements,
            DateOnly from,
            DateOnly to,
            FinancialDashboardGranularity granularity,
            decimal opening
        )
        {
            var points = new List<FinancialTrendPointResult>();
            var start = from;
            var balance = opening;
            while (start <= to)
            {
                var end = PeriodEnd(start, to, granularity);
                var set = movements
                    .Where(m => m.MovementDate >= start && m.MovementDate <= end)
                    .ToArray();
                var input = set.Where(IsSale).Sum(m => m.TotalAmount);
                var output = set.Where(IsExpense).Sum(m => m.TotalAmount);
                balance += input - output;
                points.Add(new(start, end, input, output, input - output, balance));
                start = end.AddDays(1);
            }
            return points;
        }

        private static DateOnly PeriodEnd(
            DateOnly start,
            DateOnly limit,
            FinancialDashboardGranularity g
        )
        {
            DateOnly end = g switch
            {
                FinancialDashboardGranularity.Day => start,
                FinancialDashboardGranularity.Week => start.AddDays(6),
                FinancialDashboardGranularity.Month => new DateOnly(
                    start.Year,
                    start.Month,
                    DateTime.DaysInMonth(start.Year, start.Month)
                ),
                FinancialDashboardGranularity.Year => new DateOnly(start.Year, 12, 31),
                _ => start,
            };
            return end > limit ? limit : end;
        }

        private static IReadOnlyCollection<CategoryAmountResult> MapCategories(
            IEnumerable<FinancialDashboardCategoryProjection> categories,
            int type
        )
        {
            var data = categories
                .Where(c => c.Type == type)
                .OrderByDescending(c => c.Amount)
                .ThenBy(c => c.CategoryId)
                .ToArray();
            var total = data.Sum(c => c.Amount);
            return data.Select(c => new CategoryAmountResult(
                    c.CategoryId,
                    c.CategoryName,
                    c.CategoryIsActive,
                    c.Amount,
                    total == 0m
                        ? 0m
                        : decimal.Round(c.Amount / total * 100m, 2, MidpointRounding.AwayFromZero)
                ))
                .ToArray();
        }
    }
}
