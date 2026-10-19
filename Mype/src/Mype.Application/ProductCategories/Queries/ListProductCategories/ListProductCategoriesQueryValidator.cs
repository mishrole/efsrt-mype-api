using FluentValidation;
using Mype.Domain.ProductCategories.Constraints;
using Mype.Shared.Constants;

namespace Mype.Application.ProductCategories.Queries.ListProductCategories
{
    public sealed class ListProductCategoriesQueryValidator
        : AbstractValidator<ListProductCategoriesQuery>
    {
        public ListProductCategoriesQueryValidator()
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
                .MaximumLength(ProductCategoryConstraints.NormalizedNameMaxLength)
                .WithMessage(ValidationMessages.MaximumLength)
                .When(query => !string.IsNullOrWhiteSpace(query.Search))
                .WithName("Búsqueda");
        }
    }
}
