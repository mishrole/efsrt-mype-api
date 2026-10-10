using FluentValidation;
using Mype.Domain.FinancialMovements.Constraints;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.UpdateExpenseItem
{
    public sealed class UpdateExpenseItemCommandValidator
        : AbstractValidator<UpdateExpenseItemCommand>
    {
        public UpdateExpenseItemCommandValidator()
        {
            RuleFor(command => command.BusinessId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required);
            RuleFor(command => command.MovementId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required);
            RuleFor(command => command.ItemId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(command => command.CurrentUserId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required);
            RuleFor(command => command.CategoryId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required);
            RuleFor(command => command.Description)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .MaximumLength(FinancialMovementItemConstraints.DescriptionMaxLength)
                .WithMessage(ValidationMessages.MaximumLength);
            RuleFor(command => command.Quantity)
                .GreaterThan(0)
                .PrecisionScale(
                    FinancialMovementItemConstraints.QuantityPrecision,
                    FinancialMovementItemConstraints.QuantityScale,
                    true
                );
            RuleFor(command => command.UnitAmount)
                .GreaterThanOrEqualTo(0)
                .PrecisionScale(
                    FinancialMovementItemConstraints.AmountPrecision,
                    FinancialMovementItemConstraints.AmountScale,
                    true
                );
            RuleFor(command => command.MovementVersion).NotEmpty();
            RuleFor(command => command.ItemVersion).NotEmpty();
        }
    }
}
