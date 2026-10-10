using FluentValidation;
using Mype.Domain.Products.Constraints;
using Mype.Shared.Constants;

namespace Mype.Application.Products.Commands.UpdateProduct
{
    public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
    {
        private const decimal MaximumAmount = 9999999999999999.99m;

        public UpdateProductCommandValidator()
        {
            RuleFor(command => command.BusinessId).NotEmpty().WithMessage(ValidationMessages.Required).WithName("Negocio");
            RuleFor(command => command.ProductId).NotEmpty().WithMessage(ValidationMessages.Required).WithName("Producto");
            RuleFor(command => command.CurrentUserId).NotEmpty().WithMessage(ValidationMessages.Required).WithName("Usuario");
            RuleFor(command => command.CategoryId).NotEmpty().WithMessage(ValidationMessages.Required).WithName("Categoría");
            RuleFor(command => command.Version).NotEmpty().WithMessage(ValidationMessages.Required).WithName("Versión");
            RuleFor(command => command.Name)
                .NotEmpty().WithMessage(ValidationMessages.Required)
                .MaximumLength(ProductConstraints.NameMaxLength).WithMessage(ValidationMessages.MaximumLength)
                .WithName("Nombre");
            RuleFor(command => command.SalePrice)
                .GreaterThanOrEqualTo(0).WithMessage(ValidationMessages.NonNegative)
                .LessThanOrEqualTo(MaximumAmount).WithMessage(ValidationMessages.DecimalPrecision)
                .Must(HasValidScale).WithMessage(ValidationMessages.DecimalScale)
                .WithName("Precio de venta");
            RuleFor(command => command.UnitCost)
                .GreaterThanOrEqualTo(0).WithMessage(ValidationMessages.NonNegative)
                .LessThanOrEqualTo(MaximumAmount).WithMessage(ValidationMessages.DecimalPrecision)
                .Must(HasValidScale).WithMessage(ValidationMessages.DecimalScale)
                .WithName("Coste unitario");
        }

        private static bool HasValidScale(decimal value) => decimal.Round(value, ProductConstraints.AmountScale) == value;
    }
}
