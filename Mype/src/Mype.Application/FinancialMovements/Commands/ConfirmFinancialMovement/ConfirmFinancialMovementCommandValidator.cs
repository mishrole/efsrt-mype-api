using System.Linq;
using FluentValidation;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.ConfirmFinancialMovement
{
    public sealed class ConfirmFinancialMovementCommandValidator
        : AbstractValidator<ConfirmFinancialMovementCommand>
    {
        public ConfirmFinancialMovementCommandValidator()
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
            RuleFor(command => command.ItemVersions)
                .NotNull()
                .WithMessage(ValidationMessages.Required);
            RuleForEach(command => command.ItemVersions)
                .ChildRules(item =>
                {
                    item.RuleFor(version => version.ItemId)
                        .NotEmpty()
                        .WithMessage(ValidationMessages.Required);
                    item.RuleFor(version => version.Version).NotEmpty();
                });
            RuleFor(command => command.ItemVersions)
                .Must(versions =>
                    versions == null
                    || versions.Select(version => version.ItemId).Distinct().Count()
                        == versions.Count
                );
        }
    }
}
