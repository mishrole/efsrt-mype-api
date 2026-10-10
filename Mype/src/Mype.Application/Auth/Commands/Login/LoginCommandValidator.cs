using FluentValidation;
using Mype.Domain.Users.Constraints;
using Mype.Shared.Constants;

namespace Mype.Application.Auth.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(command => command.Email)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .MaximumLength(UserConstraints.EmailMaxLength)
                .WithMessage(ValidationMessages.MaximumLength)
                .EmailAddress()
                .WithMessage(ValidationMessages.Invalid)
                .WithName("Correo electrónico");

            RuleFor(command => command.Password)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Contraseña");
        }
    }
}
