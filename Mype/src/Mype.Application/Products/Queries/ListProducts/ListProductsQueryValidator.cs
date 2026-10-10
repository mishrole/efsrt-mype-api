using System;
using FluentValidation;
using Mype.Domain.Products.Constraints;
using Mype.Shared.Constants;

namespace Mype.Application.Products.Queries.ListProducts
{
    public sealed class ListProductsQueryValidator : AbstractValidator<ListProductsQuery>
    {
        public ListProductsQueryValidator()
        {
            RuleFor(query => query.BusinessId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Negocio");

            RuleFor(query => query.CurrentUserId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Usuario");

            RuleFor(query => query.Search)
                .MaximumLength(ProductConstraints.NormalizedNameMaxLength)
                .WithMessage(ValidationMessages.MaximumLength)
                .When(query => !string.IsNullOrWhiteSpace(query.Search))
                .WithName("Búsqueda");

            RuleFor(query => query.CategoryId)
                .Must(categoryId => !categoryId.HasValue || categoryId.Value != Guid.Empty)
                .WithMessage(ValidationMessages.Invalid)
                .WithName("Categoría");
        }
    }
}
