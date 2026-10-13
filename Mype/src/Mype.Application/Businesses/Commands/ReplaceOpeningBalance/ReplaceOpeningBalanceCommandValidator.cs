using FluentValidation;
using Mype.Domain.FinancialMovements.Constraints;
using Mype.Shared.Constants;

namespace Mype.Application.Businesses.Commands.ReplaceOpeningBalance
{
    public sealed class ReplaceOpeningBalanceCommandValidator
        : AbstractValidator<ReplaceOpeningBalanceCommand>
    {
        public ReplaceOpeningBalanceCommandValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.CurrentUserId).NotEmpty();
            RuleFor(x => x.MovementVersion).NotEmpty();
            RuleFor(x => x.Reason)
                .NotEmpty()
                .MinimumLength(
                    FinancialMovementConstraints.OpeningBalanceReplacementReasonMinLength
                )
                .MaximumLength(
                    FinancialMovementConstraints.OpeningBalanceReplacementReasonMaxLength
                );
        }
    }
}
