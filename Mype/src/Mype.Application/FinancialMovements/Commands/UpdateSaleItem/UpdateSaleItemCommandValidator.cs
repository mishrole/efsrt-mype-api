using FluentValidation;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.UpdateSaleItem
{
    public sealed class UpdateSaleItemCommandValidator : AbstractValidator<UpdateSaleItemCommand>
    {
        public UpdateSaleItemCommandValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.MovementId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.CurrentUserId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.ItemId).NotEmpty();
            RuleFor(x => x.ProductId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0).PrecisionScale(18, 4, true);
            RuleFor(x => x.UnitAmount).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true);
            RuleFor(x => x.MovementVersion).NotEmpty();
            RuleFor(x => x.ItemVersion).NotEmpty();
        }
    }
}
