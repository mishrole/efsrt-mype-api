using FluentValidation;
using Mype.Domain.FinancialMovements.Constraints;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.UpdateFinancialMovementDraft
{
    public sealed class UpdateFinancialMovementDraftCommandValidator
        : AbstractValidator<UpdateFinancialMovementDraftCommand>
    {
        public UpdateFinancialMovementDraftCommandValidator()
        {
            RuleFor(command => command.BusinessId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Negocio");
            RuleFor(command => command.MovementId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Movimiento");
            RuleFor(command => command.CurrentUserId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Usuario");
            RuleFor(command => command.MovementDate)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Fecha");
            RuleFor(command => command.Description)
                .MaximumLength(FinancialMovementConstraints.DescriptionMaxLength)
                .WithMessage(ValidationMessages.MaximumLength)
                .WithName("Descripción");
            RuleFor(command => command.Version)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Versión");
        }
    }
}
