using FluentValidation;
using Mype.Shared.Constants;

namespace Mype.Application.ProductCategories.Commands.ReactivateProductCategory
{
    public sealed class ReactivateProductCategoryCommandValidator
        : AbstractValidator<ReactivateProductCategoryCommand>
    {
        public ReactivateProductCategoryCommandValidator()
        {
            RuleFor(command => command.BusinessId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Negocio");
            RuleFor(command => command.ProductCategoryId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Categoría de producto");
            RuleFor(command => command.CurrentUserId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Usuario");
            RuleFor(command => command.Version)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Versión");
        }
    }
}
