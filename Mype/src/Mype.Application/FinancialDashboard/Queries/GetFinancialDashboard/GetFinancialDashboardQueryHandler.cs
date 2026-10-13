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
                var averageSaleAmount = CalculateAverageSaleAmount(inflow, sales);
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
                        averageSaleAmount,
                        trend,
                        MapCategories(categories, FinancialMovementType.Sale),
                        MapCategories(categories, FinancialMovementType.Expense)
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

        private static decimal CalculateAverageSaleAmount(decimal inflow, int sales)
        {
            if (sales == 0)
            {
                return 0m;
            }

            return decimal.Round(inflow / sales, 2, MidpointRounding.AwayFromZero);
        }

        private static bool IsSale(FinancialDashboardMovementProjection m) =>
            m.Type == (int)FinancialMovementType.Sale;

        private static bool IsExpense(FinancialDashboardMovementProjection m) =>
            m.Type == (int)FinancialMovementType.Expense;

        private static decimal SignedAmount(FinancialDashboardMovementProjection movement)
        {
            if (IsSale(movement))
            {
                return movement.TotalAmount;
            }

            if (IsExpense(movement))
            {
                return -movement.TotalAmount;
            }

            return 0m;
        }

        private static FinancialDashboardGranularity ResolveGranularity(
            FinancialDashboardGranularity requestedGranularity,
            DateOnly dateFrom,
            DateOnly dateTo
        )
        {
            if (requestedGranularity != FinancialDashboardGranularity.Auto)
            {
                return requestedGranularity;
            }

            var inclusiveDays = dateTo.DayNumber - dateFrom.DayNumber + 1;

            if (inclusiveDays <= 31)
            {
                return FinancialDashboardGranularity.Day;
            }

            if (inclusiveDays <= 180)
            {
                return FinancialDashboardGranularity.Week;
            }

            if (inclusiveDays <= 1095)
            {
                return FinancialDashboardGranularity.Month;
            }

            return FinancialDashboardGranularity.Year;
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
            FinancialDashboardGranularity granularity
        )
        {
            DateOnly end;

            switch (granularity)
            {
                case FinancialDashboardGranularity.Day:
                    end = start;
                    break;

                case FinancialDashboardGranularity.Week:
                    end = CalculateWeekEnd(start);
                    break;

                case FinancialDashboardGranularity.Month:
                    end = new DateOnly(
                        start.Year,
                        start.Month,
                        DateTime.DaysInMonth(start.Year, start.Month)
                    );
                    break;

                case FinancialDashboardGranularity.Year:
                    end = new DateOnly(start.Year, 12, 31);
                    break;

                default:
                    end = start;
                    break;
            }

            if (end > limit)
            {
                return limit;
            }

            return end;
        }

        private static DateOnly CalculateWeekEnd(DateOnly start)
        {
            const int DaysInWeek = 7;

            var daysFromMonday = ((int)start.DayOfWeek + 6) % DaysInWeek;

            var daysUntilSunday = DaysInWeek - 1 - daysFromMonday;

            return start.AddDays(daysUntilSunday);
        }

        private static IReadOnlyCollection<CategoryAmountResult> MapCategories(
            IEnumerable<FinancialDashboardCategoryProjection> categories,
            FinancialMovementType movementType
        )
        {
            var categoriesByAmount = categories
                .Where(category => category.Type == (int)movementType)
                .OrderByDescending(category => category.Amount)
                .ThenBy(category => category.CategoryId)
                .ToArray();

            var totalAmount = categoriesByAmount.Sum(category => category.Amount);

            return categoriesByAmount
                .Select(category => MapCategory(category, totalAmount))
                .ToArray();
        }

        private static CategoryAmountResult MapCategory(
            FinancialDashboardCategoryProjection category,
            decimal totalAmount
        )
        {
            var percentage = CalculatePercentage(category.Amount, totalAmount);

            return new CategoryAmountResult(
                category.CategoryId,
                category.CategoryName,
                category.CategoryIsActive,
                category.Amount,
                percentage
            );
        }

        private static decimal CalculatePercentage(decimal amount, decimal totalAmount)
        {
            if (totalAmount == 0m)
            {
                return 0m;
            }

            return decimal.Round(amount / totalAmount * 100m, 2, MidpointRounding.AwayFromZero);
        }
    }
}
