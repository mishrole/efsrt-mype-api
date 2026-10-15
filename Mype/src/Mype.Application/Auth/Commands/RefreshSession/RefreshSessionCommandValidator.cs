using FluentValidation;
using Mype.Shared.Constants;

namespace Mype.Application.Auth.Commands.RefreshSession
{
    public sealed class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
    {
        public RefreshSessionCommandValidator()
        {
            RuleFor(command => command.RefreshToken)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Refresh token");
        }
    }
}
