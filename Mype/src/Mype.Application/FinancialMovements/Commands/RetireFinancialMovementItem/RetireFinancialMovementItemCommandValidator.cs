using FluentValidation;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.RetireFinancialMovementItem
{
    public sealed class RetireFinancialMovementItemCommandValidator
        : AbstractValidator<RetireFinancialMovementItemCommand>
    {
        public RetireFinancialMovementItemCommandValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.MovementId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.CurrentUserId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.ItemId).NotEmpty();
            RuleFor(x => x.MovementVersion).NotEmpty();
            RuleFor(x => x.ItemVersion).NotEmpty();
        }
    }
}
