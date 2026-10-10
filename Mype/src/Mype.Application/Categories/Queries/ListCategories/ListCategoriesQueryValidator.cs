using FluentValidation;
using Mype.Domain.Categories;
using Mype.Shared.Constants;

namespace Mype.Application.Categories.Queries.ListCategories
{
    public sealed class ListCategoriesQueryValidator : AbstractValidator<ListCategoriesQuery>
    {
        public ListCategoriesQueryValidator()
        {
            RuleFor(query => query.BusinessId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Negocio");

            RuleFor(query => query.CurrentUserId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Usuario");

            RuleFor(query => query.Type)
                .Must(type => !type.HasValue || IsSupportedType(type.Value))
                .WithMessage(ValidationMessages.Invalid)
                .WithName("Tipo");
        }

        private static bool IsSupportedType(CategoryType type)
        {
            return type == CategoryType.Sale || type == CategoryType.Expense;
        }
    }
}
