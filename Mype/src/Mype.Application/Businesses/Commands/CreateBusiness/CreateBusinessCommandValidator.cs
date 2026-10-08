using FluentValidation;
using Mype.Application.Businesses.Validation;
using Mype.Domain.Businesses.Constraints;
using Mype.Domain.Currencies.Constraints;
using Mype.Shared.Constants;

namespace Mype.Application.Businesses.Commands.CreateBusiness
{
    public class CreateBusinessCommandValidator
        : AbstractValidator<CreateBusinessCommand>
    {
        public CreateBusinessCommandValidator()
        {
            RuleFor(command => command.DisplayName)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .MaximumLength(
                    BusinessConstraints.DisplayNameMaxLength
                )
                .WithMessage(ValidationMessages.MaximumLength)
                .WithName("Nombre del negocio");

            RuleFor(command => command.LegalName)
                .MaximumLength(
                    BusinessConstraints.LegalNameMaxLength
                )
                .WithMessage(ValidationMessages.MaximumLength)
                .When(command =>
                    !string.IsNullOrWhiteSpace(
                        command.LegalName
                    )
                )
                .WithName("Razón social");

            RuleFor(command => command.Ruc)
                .Must(RucValidator.IsValid)
                .WithMessage(ErrorMessages.InvalidRuc)
                .When(command =>
                    !string.IsNullOrWhiteSpace(command.Ruc)
                )
                .WithName("RUC");

            RuleFor(command => command.CurrencyCode)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .Length(
                    CurrencyConstraints.CodeMaxLength
                )
                .WithMessage(ValidationMessages.Invalid)
                .WithName("Moneda");

            RuleFor(command => command.CurrentUserId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Usuario");
        }
    }
}