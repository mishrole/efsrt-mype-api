using FluentValidation;
using Mype.Shared.Constants;

namespace Mype.Application.MovementEvidences.Commands.RemoveMovementEvidence
{
    public sealed class RemoveMovementEvidenceCommandValidator
        : AbstractValidator<RemoveMovementEvidenceCommand>
    {
        public RemoveMovementEvidenceCommandValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.MovementId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.EvidenceId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.CurrentUserId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.MovementVersion).NotEmpty();
            RuleFor(x => x.EvidenceVersion).NotEmpty();
        }
    }
}
