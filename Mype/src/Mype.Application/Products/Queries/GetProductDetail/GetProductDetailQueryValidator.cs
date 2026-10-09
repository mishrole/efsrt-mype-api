using FluentValidation;
using Mype.Shared.Constants;

namespace Mype.Application.Products.Queries.GetProductDetail
{
    public sealed class
        GetProductDetailQueryValidator
        : AbstractValidator<
            GetProductDetailQuery
        >
    {
        public GetProductDetailQueryValidator()
        {
            RuleFor(query =>
                    query.BusinessId
                )
                .NotEmpty()
                .WithMessage(
                    ValidationMessages.Required
                )
                .WithName("Negocio");

            RuleFor(query =>
                    query.ProductId
                )
                .NotEmpty()
                .WithMessage(
                    ValidationMessages.Required
                )
                .WithName("Producto");

            RuleFor(query =>
                    query.CurrentUserId
                )
                .NotEmpty()
                .WithMessage(
                    ValidationMessages.Required
                )
                .WithName("Usuario");
        }
    }
}