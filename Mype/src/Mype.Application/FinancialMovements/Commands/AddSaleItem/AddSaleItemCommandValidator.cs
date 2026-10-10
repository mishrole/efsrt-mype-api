using FluentValidation;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.AddSaleItem
{
    public sealed class AddSaleItemCommandValidator : AbstractValidator<AddSaleItemCommand>
    {
        public AddSaleItemCommandValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.MovementId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.CurrentUserId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.ProductId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.Quantity).GreaterThan(0).PrecisionScale(18, 4, true);
            RuleFor(x => x.UnitAmount).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true);
            RuleFor(x => x.MovementVersion).NotEmpty();
        }
    }
}
