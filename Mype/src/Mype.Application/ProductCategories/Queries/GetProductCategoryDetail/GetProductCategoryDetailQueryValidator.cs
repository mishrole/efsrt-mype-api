using FluentValidation;
using Mype.Shared.Constants;

namespace Mype.Application.ProductCategories.Queries.GetProductCategoryDetail
{
    public sealed class GetProductCategoryDetailQueryValidator
        : AbstractValidator<GetProductCategoryDetailQuery>
    {
        public GetProductCategoryDetailQueryValidator()
        {
            RuleFor(query => query.BusinessId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Negocio");
            RuleFor(query => query.ProductCategoryId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Categoría de producto");
            RuleFor(query => query.CurrentUserId)
                .NotEmpty()
                .WithMessage(ValidationMessages.Required)
                .WithName("Usuario");
        }
    }
}
