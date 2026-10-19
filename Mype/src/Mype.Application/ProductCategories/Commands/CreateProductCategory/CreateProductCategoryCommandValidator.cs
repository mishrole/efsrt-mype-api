using FluentValidation;
using Mype.Domain.ProductCategories.Constraints;
using Mype.Shared.Constants;

namespace Mype.Application.ProductCategories.Commands.CreateProductCategory
{
    public sealed class CreateProductCategoryCommandValidator
        : AbstractValidator<CreateProductCategoryCommand>
    {
        public CreateProductCategoryCommandValidator()
        {
            RuleFor(command => command.BusinessId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Negocio");

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
                .Length(ProductCategoryConstraints.ColorLength)
                .WithMessage(ValidationMessages.Invalid)
                .Matches(ProductCategoryConstraints.ColorPattern)
                .WithMessage(ValidationMessages.Invalid)
                .When(command => !string.IsNullOrWhiteSpace(command.Color))
                .WithName("Color");
        }
    }
}
