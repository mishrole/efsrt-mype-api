using FluentValidation;
using Mype.Domain.Products.Constraints;
using Mype.Shared.Constants;

namespace Mype.Application.Products.Commands.CreateProduct
{
    public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
    {
        private const decimal MaximumAmount = 9999999999999999.99m;

        public CreateProductCommandValidator()
        {
            RuleFor(command => command.BusinessId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Negocio");

            RuleFor(command => command.CurrentUserId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Usuario");

            RuleFor(command => command.CategoryId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Categoría");

            RuleFor(command => command.Name)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .MaximumLength(ProductConstraints.NameMaxLength)
                .WithMessage(ValidationMessages.MaximumLength)
                .WithName("Nombre");

            RuleFor(command => command.SalePrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage(ValidationMessages.NonNegative)
                .LessThanOrEqualTo(MaximumAmount)
                .WithMessage(ValidationMessages.DecimalPrecision)
                .Must(HasValidScale)
                .WithMessage(ValidationMessages.DecimalScale)
                .WithName("Precio de venta");

            RuleFor(command => command.UnitCost)
                .GreaterThanOrEqualTo(0)
                .WithMessage(ValidationMessages.NonNegative)
                .LessThanOrEqualTo(MaximumAmount)
                .WithMessage(ValidationMessages.DecimalPrecision)
                .Must(HasValidScale)
                .WithMessage(ValidationMessages.DecimalScale)
                .WithName("Coste unitario");
        }

        private static bool HasValidScale(decimal value)
        {
            return decimal.Round(value, ProductConstraints.AmountScale) == value;
        }
    }
}
