using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

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
            .WithMessage("Display name is required.")
            .MaximumLength(DisplayNameMaxLength)
            .WithMessage(
                $"Display name must not exceed {DisplayNameMaxLength} characters."
            );

            RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(EmailMaxLength)
            .WithMessage(
                $"Email must not exceed {EmailMaxLength} characters."
            )
            .EmailAddress()
            .WithMessage("Email is not valid.");

            RuleFor(command => command.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(PasswordMinLength)
            .WithMessage(
                $"Password must contain at least {PasswordMinLength} characters."
            )
            .Matches("[A-Za-z]")
            .WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]")
            .WithMessage("Password must contain at least one number.");

            RuleFor(command => command.PasswordConfirmation)
            .NotEmpty()
            .WithMessage("Password confirmation is required.")
            .Equal(command => command.Password)
            .WithMessage("Passwords do not match.");
        }
    }
}
