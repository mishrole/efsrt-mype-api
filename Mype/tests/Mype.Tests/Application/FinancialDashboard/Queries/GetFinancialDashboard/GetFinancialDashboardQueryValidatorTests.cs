using System;
using FluentAssertions;
using FluentValidation.TestHelper;
using Mype.Application.FinancialDashboard.Enums;
using Mype.Application.FinancialDashboard.Queries.GetFinancialDashboard;

namespace Mype.Tests.Application.FinancialDashboard.Queries.GetFinancialDashboard
{
    public sealed class GetFinancialDashboardQueryValidatorTests
    {
        private readonly GetFinancialDashboardQueryValidator _validator = new();

        [Theory]
        [InlineData(FinancialDashboardGranularity.Auto)]
        [InlineData(FinancialDashboardGranularity.Day)]
        [InlineData(FinancialDashboardGranularity.Week)]
        [InlineData(FinancialDashboardGranularity.Month)]
        [InlineData(FinancialDashboardGranularity.Year)]
        public void Validate_Should_Accept_Complete_Period_And_Granularity(
            FinancialDashboardGranularity granularity
        )
        {
            var result = _validator.TestValidate(ValidQuery(granularity));
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_Should_Accept_Both_Dates_Omitted()
        {
            var query = ValidQuery();
            query.DateFrom = null;
            query.DateTo = null;
            _validator.TestValidate(query).IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void Validate_Should_Reject_Incomplete_Period(bool hasFrom, bool hasTo)
        {
            var query = ValidQuery();
            query.DateFrom = hasFrom ? new DateOnly(2026, 10, 1) : null;
            query.DateTo = hasTo ? new DateOnly(2026, 10, 31) : null;
            _validator.TestValidate(query).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_Should_Reject_Inverted_Period()
        {
            var query = ValidQuery();
            query.DateFrom = new DateOnly(2026, 10, 31);
            query.DateTo = new DateOnly(2026, 10, 1);
            _validator.TestValidate(query).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_Should_Reject_Empty_Ids_And_Invalid_Granularity()
        {
            var query = ValidQuery();
            query.BusinessId = Guid.Empty;
            query.CurrentUserId = Guid.Empty;
            query.Granularity = (FinancialDashboardGranularity)999;
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(current => current.BusinessId);
            result.ShouldHaveValidationErrorFor(current => current.CurrentUserId);
            result.ShouldHaveValidationErrorFor(current => current.Granularity);
        }

        private static GetFinancialDashboardQuery ValidQuery(
            FinancialDashboardGranularity granularity = FinancialDashboardGranularity.Auto
        ) =>
            new()
            {
                BusinessId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                DateFrom = new DateOnly(2026, 10, 1),
                DateTo = new DateOnly(2026, 10, 31),
                Granularity = granularity,
            };
    }
}
