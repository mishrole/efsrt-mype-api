using FluentValidation;
using Mype.Application.FinancialMovements.Models;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Queries.GetFinancialDashboard
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
            RuleFor(query => query)
                .Must(query =>
                    !query.DateFrom.HasValue
                    || !query.DateTo.HasValue
                    || query.DateFrom.Value <= query.DateTo.Value
                );
        }
    }
}
