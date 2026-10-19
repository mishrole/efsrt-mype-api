using FluentValidation;
using Mype.Domain.ProductCategories.Constraints;
using Mype.Shared.Constants;

namespace Mype.Application.ProductCategories.Commands.UpdateProductCategory
{
    public sealed class UpdateProductCategoryCommandValidator
        : AbstractValidator<UpdateProductCategoryCommand>
    {
        public UpdateProductCategoryCommandValidator()
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
            RuleFor(command => command.Name)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .MaximumLength(ProductCategoryConstraints.NameMaxLength)
                .WithMessage(ValidationMessages.MaximumLength)
                .WithName("Nombre");
            RuleFor(command => command.Color)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .Length(ProductCategoryConstraints.ColorLength)
                .WithMessage(ValidationMessages.Invalid)
                .Matches(ProductCategoryConstraints.ColorPattern)
                .WithMessage(ValidationMessages.Invalid)
                .WithName("Color");
            RuleFor(command => command.Version)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Versión");
        }
    }
}
