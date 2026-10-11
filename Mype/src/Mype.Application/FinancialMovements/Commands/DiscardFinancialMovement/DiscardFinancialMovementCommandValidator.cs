using System.Linq;
using FluentValidation;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.DiscardFinancialMovement
{
    public sealed class DiscardFinancialMovementCommandValidator
        : AbstractValidator<DiscardFinancialMovementCommand>
    {
        public DiscardFinancialMovementCommandValidator()
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
            RuleFor(command => command.EvidenceVersions)
                .NotNull()
                .WithMessage(ValidationMessages.Required);
            RuleForEach(command => command.EvidenceVersions)
                .ChildRules(evidence =>
                {
                    evidence
                        .RuleFor(version => version.EvidenceId)
                        .NotEmpty()
                        .WithMessage(ValidationMessages.Required);
                    evidence.RuleFor(version => version.Version).NotEmpty();
                });
            RuleFor(command => command.EvidenceVersions)
                .Must(versions =>
                    versions == null
                    || versions.Select(version => version.EvidenceId).Distinct().Count()
                        == versions.Count
                );
        }
    }
}
