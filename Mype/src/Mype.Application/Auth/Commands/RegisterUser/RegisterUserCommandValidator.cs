using FluentValidation;
using Mype.Shared.Constants;

namespace Mype.Application.Auth.Commands.RegisterUser
{
    public class RegisterUserCommandValidator
        : AbstractValidator<RegisterUserCommand>
    {
        private const int DisplayNameMaxLength = 150;
        private const int EmailMaxLength = 320;
        private const int PasswordMinLength = 8;

        public RegisterUserCommandValidator()
        {
            RuleFor(command => command.DisplayName)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .MaximumLength(DisplayNameMaxLength)
                .WithMessage(ValidationMessages.MaximumLength)
                .WithName("Nombre");

            RuleFor(command => command.Email)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .MaximumLength(EmailMaxLength)
                .WithMessage(ValidationMessages.MaximumLength)
                .EmailAddress()
                .WithMessage(ValidationMessages.Invalid)
                .WithName("Correo electrónico");

            RuleFor(command => command.Password)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .MinimumLength(PasswordMinLength)
                .WithMessage(ValidationMessages.MinimumLength)
                .Matches("[A-Za-z]")
                .WithMessage(ValidationMessages.MustContainLetter)
                .Matches("[0-9]")
                .WithMessage(ValidationMessages.MustContainNumber)
                .WithName("Contraseña");

            RuleFor(command => command.PasswordConfirmation)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .Equal(command => command.Password)
                .WithMessage(ValidationMessages.PasswordsDoNotMatch)
                .WithName("Confirmación de contraseña");
        }
    }
}