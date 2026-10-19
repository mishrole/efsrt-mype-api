using FluentAssertions;
using Mype.Application.ProductCategories;

namespace Mype.Tests.Application.ProductCategories
{
    public sealed class ProductCategoryNameNormalizerTests
    {
        [Fact]
        public void Normalize_Should_Trim_Collapse_And_Uppercase()
        {
            ProductCategoryNameNormalizer
                .NormalizeName("  Bebidas   frías ")
                .Should()
                .Be("Bebidas frías");
            ProductCategoryNameNormalizer
                .NormalizeForComparison("  Bebidas   frías ")
                .Should()
                .Be("BEBIDAS FRÍAS");
        }
    }
}
