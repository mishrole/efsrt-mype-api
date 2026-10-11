using FluentValidation;
using Mype.Shared.Constants;

namespace Mype.Application.MovementEvidences.Commands.ReplaceMovementEvidence
{
    public sealed class ReplaceMovementEvidenceCommandValidator
        : AbstractValidator<ReplaceMovementEvidenceCommand>
    {
        public ReplaceMovementEvidenceCommandValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.MovementId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.EvidenceId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.CurrentUserId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.MovementVersion).NotEmpty();
            RuleFor(x => x.EvidenceVersion).NotEmpty();
            RuleFor(x => x.File).NotNull().WithMessage(ValidationMessages.Required);
        }
    }
}
