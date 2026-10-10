using FluentValidation;
using Mype.Shared.Constants;

namespace Mype.Application.Products.Commands.DeactivateProduct
{
    public sealed class DeactivateProductCommandValidator : AbstractValidator<DeactivateProductCommand>
    {
        public DeactivateProductCommandValidator()
        {
            RuleFor(command => command.BusinessId).NotEmpty().WithMessage(ValidationMessages.Required).WithName("Negocio");
            RuleFor(command => command.ProductId).NotEmpty().WithMessage(ValidationMessages.Required).WithName("Producto");
            RuleFor(command => command.CurrentUserId).NotEmpty().WithMessage(ValidationMessages.Required).WithName("Usuario");
            RuleFor(command => command.Version).NotEmpty().WithMessage(ValidationMessages.Required).WithName("Versión");
        }
    }
}
