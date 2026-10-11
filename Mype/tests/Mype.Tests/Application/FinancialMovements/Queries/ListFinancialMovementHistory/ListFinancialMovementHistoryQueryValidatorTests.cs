using System;
using FluentAssertions;
using FluentValidation.TestHelper;
using Mype.Application.FinancialMovements.Queries.ListFinancialMovementHistory;
using Mype.Domain.FinancialMovements;

namespace Mype.Tests.Application.FinancialMovements.Queries.ListFinancialMovementHistory
{
    public sealed class ListFinancialMovementHistoryQueryValidatorTests
    {
        [Fact]
        public void Validate_Should_Accept_Defaults_And_All_Filters()
        {
            var query = new ListFinancialMovementHistoryQuery
            {
                BusinessId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                Type = FinancialMovementType.Sale,
                Status = FinancialMovementStatus.Confirmed,
                DateFrom = new DateOnly(2026, 10, 1),
                DateTo = new DateOnly(2026, 10, 31),
                CategoryId = Guid.NewGuid(),
                Page = 1,
                PageSize = 100,
            };

            new ListFinancialMovementHistoryQueryValidator()
                .TestValidate(query)
                .IsValid.Should()
                .BeTrue();
        }

        [Theory]
        [InlineData(0, 20)]
        [InlineData(1, 0)]
        [InlineData(1, 101)]
        public void Validate_Should_Reject_Invalid_Pagination(int page, int pageSize)
        {
            var result = new ListFinancialMovementHistoryQueryValidator().TestValidate(
                new ListFinancialMovementHistoryQuery
                {
                    BusinessId = Guid.NewGuid(),
                    CurrentUserId = Guid.NewGuid(),
                    Page = page,
                    PageSize = pageSize,
                }
            );

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_Should_Reject_Inverted_Date_Range()
        {
            var result = new ListFinancialMovementHistoryQueryValidator().TestValidate(
                new ListFinancialMovementHistoryQuery
                {
                    BusinessId = Guid.NewGuid(),
                    CurrentUserId = Guid.NewGuid(),
                    Page = 1,
                    PageSize = 20,
                    DateFrom = new DateOnly(2026, 10, 31),
                    DateTo = new DateOnly(2026, 10, 1),
                }
            );

            result.IsValid.Should().BeFalse();
        }
    }
}
