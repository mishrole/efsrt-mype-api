using FluentValidation;
using Mype.Domain.FinancialMovements.Constraints;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.CreateFinancialMovement
{
    public sealed class CreateFinancialMovementCommandValidator : AbstractValidator<CreateFinancialMovementCommand>
    {
        public CreateFinancialMovementCommandValidator()
        {
            RuleFor(command => command.BusinessId).NotEmpty().WithMessage(ValidationMessages.Required).WithName("Negocio");
            RuleFor(command => command.CurrentUserId).NotEmpty().WithMessage(ValidationMessages.Required).WithName("Usuario");
            RuleFor(command => command.Type).IsInEnum().WithMessage(ValidationMessages.Invalid).WithName("Tipo");
            RuleFor(command => command.MovementDate).NotEmpty().WithMessage(ValidationMessages.Required).WithName("Fecha");
            RuleFor(command => command.Description).MaximumLength(FinancialMovementConstraints.DescriptionMaxLength).WithMessage(ValidationMessages.MaximumLength).WithName("Descripción");
        }
    }
}
