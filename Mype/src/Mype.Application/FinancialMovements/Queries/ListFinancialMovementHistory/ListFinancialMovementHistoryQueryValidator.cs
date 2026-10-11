using FluentValidation;
using Mype.Domain.FinancialMovements;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Queries.ListFinancialMovementHistory
{
    public sealed class ListFinancialMovementHistoryQueryValidator
        : AbstractValidator<ListFinancialMovementHistoryQuery>
    {
        public const int MaximumPageSize = 100;

        public ListFinancialMovementHistoryQueryValidator()
        {
            RuleFor(query => query.BusinessId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(query => query.CurrentUserId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required);
            RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
            RuleFor(query => query.PageSize).InclusiveBetween(1, MaximumPageSize);
            RuleFor(query => query.Type).IsInEnum().When(query => query.Type.HasValue);
            RuleFor(query => query.Status).IsInEnum().When(query => query.Status.HasValue);
            RuleFor(query => query)
                .Must(query =>
                    !query.DateFrom.HasValue
                    || !query.DateTo.HasValue
                    || query.DateFrom.Value <= query.DateTo.Value
                );
        }
    }
}
