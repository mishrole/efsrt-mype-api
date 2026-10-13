using System;
using FluentValidation;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialDashboard.Queries.GetFinancialDashboard
{
    public sealed class GetFinancialDashboardQueryValidator
        : AbstractValidator<GetFinancialDashboardQuery>
    {
        public GetFinancialDashboardQueryValidator()
        {
            RuleFor(query => query.BusinessId).NotEmpty().WithMessage(ValidationMessages.Required);

            RuleFor(query => query.CurrentUserId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required);

            RuleFor(query => query.Granularity).IsInEnum();

            RuleFor(query => query.DateFrom)
                .Must(HaveBothDatesOrNeither)
                .WithMessage(ValidationMessages.BothValuesMustBeProvidedOrOmitted)
                .WithName(
                    $"{nameof(GetFinancialDashboardQuery.DateFrom)} y "
                        + nameof(GetFinancialDashboardQuery.DateTo)
                );

            RuleFor(query => query.DateFrom)
                .Must(HaveValidDateOrder)
                .WithMessage(ValidationMessages.StartDateMustNotBeAfterEndDate)
                .WithName(nameof(GetFinancialDashboardQuery.DateFrom));
        }

        private static bool HaveBothDatesOrNeither(GetFinancialDashboardQuery query, DateOnly? _) =>
            query.DateFrom.HasValue == query.DateTo.HasValue;

        private static bool HaveValidDateOrder(
            GetFinancialDashboardQuery query,
            DateOnly? dateFrom
        ) => !dateFrom.HasValue || !query.DateTo.HasValue || dateFrom.Value <= query.DateTo.Value;
    }
}
