using FluentValidation;
using Mype.Domain.Categories;

namespace Mype.Application.Categories.Queries.ListCategories
{
    public sealed class ListCategoriesQueryValidator : AbstractValidator<ListCategoriesQuery>
    {
        public ListCategoriesQueryValidator()
        {
            RuleFor(query => query.BusinessId)
                .NotEmpty();

            RuleFor(query => query.CurrentUserId)
                .NotEmpty();

            RuleFor(query => query.Type)
                .Must(type =>
                    !type.HasValue ||
                    IsSupportedType(type.Value)
                );
        }

        private static bool IsSupportedType(
            CategoryType type
        )
        {
            return
                type == CategoryType.Sale ||
                type == CategoryType.Expense;
        }
    }
}