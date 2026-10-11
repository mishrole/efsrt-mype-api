using FluentValidation;
using Mype.Shared.Constants;

namespace Mype.Application.MovementEvidences.Commands.AttachMovementEvidence
{
    public sealed class AttachMovementEvidenceCommandValidator
        : AbstractValidator<AttachMovementEvidenceCommand>
    {
        public AttachMovementEvidenceCommandValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.MovementId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.CurrentUserId).NotEmpty().WithMessage(ValidationMessages.Required);
            RuleFor(x => x.MovementVersion).NotEmpty();
            RuleFor(x => x.File).NotNull().WithMessage(ValidationMessages.Required);
        }
    }
}
