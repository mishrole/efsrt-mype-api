using FluentValidation;
using Mype.Domain.FinancialMovements.Constraints;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.CancelFinancialMovement
{
    public sealed class CancelFinancialMovementCommandValidator
        : AbstractValidator<CancelFinancialMovementCommand>
    {
        public CancelFinancialMovementCommandValidator()
        {
            RuleFor(command => command.BusinessId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required);
            RuleFor(command => command.MovementId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required);
            RuleFor(command => command.CurrentUserId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required);
            RuleFor(command => command.MovementVersion).NotEmpty();
            RuleFor(command => command.Reason)
                .Must(reason =>
                    !string.IsNullOrWhiteSpace(CancellationReasonNormalizer.Normalize(reason))
                )
                .WithMessage(ValidationMessages.Required)
                .Must(reason =>
                {
                    var normalized = CancellationReasonNormalizer.Normalize(reason);
                    return normalized == null
                        || normalized.Length
                            >= FinancialMovementConstraints.CancellationReasonMinLength;
                })
                .WithMessage(ValidationMessages.MinimumLength)
                .Must(reason =>
                {
                    var normalized = CancellationReasonNormalizer.Normalize(reason);
                    return normalized == null
                        || normalized.Length
                            <= FinancialMovementConstraints.CancellationReasonMaxLength;
                })
                .WithMessage(ValidationMessages.MaximumLength);
        }
    }
}
