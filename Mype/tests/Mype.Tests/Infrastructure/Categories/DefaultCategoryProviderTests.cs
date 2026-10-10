using System.Linq;
using FluentAssertions;
using Mype.Domain.Categories;
using Mype.Infrastructure.Categories;

namespace Mype.Tests.Infrastructure.Categories
{
    public class DefaultCategoryProviderTests
    {
        private readonly DefaultCategoryProvider _provider = new();

        [Fact]
        public void GetDefaultCategories_Should_Return_Ten_Categories()
        {
            var result = _provider.GetDefaultCategories();

            result.Should().HaveCount(10);
        }

        [Fact]
        public void GetDefaultCategories_Should_Return_Three_Sale_Categories()
        {
            var result = _provider.GetDefaultCategories();

            result.Count(category => category.Type == CategoryType.Sale).Should().Be(3);
        }

        [Fact]
        public void GetDefaultCategories_Should_Return_Seven_Expense_Categories()
        {
            var result = _provider.GetDefaultCategories();

            result.Count(category => category.Type == CategoryType.Expense).Should().Be(7);
        }

        [Fact]
        public void GetDefaultCategories_Should_Not_Contain_Duplicates_Within_Same_Type()
        {
            var result = _provider.GetDefaultCategories();

            result
                .Select(category => new
                {
                    category.Type,
                    Name = category.Name.Trim().ToUpperInvariant(),
                })
                .Should()
                .OnlyHaveUniqueItems();
        }

        [Fact]
        public void GetDefaultCategories_Should_Contain_Expected_Sale_Categories()
        {
            var result = _provider.GetDefaultCategories();

            result
                .Should()
                .ContainEquivalentOf(new { Name = "Productos", Type = CategoryType.Sale });

            result
                .Should()
                .ContainEquivalentOf(new { Name = "Servicios", Type = CategoryType.Sale });

            result
                .Should()
                .ContainEquivalentOf(new { Name = "Otros ingresos", Type = CategoryType.Sale });
        }

        [Fact]
        public void GetDefaultCategories_Should_Return_Stable_Collection()
        {
            var firstResult = _provider.GetDefaultCategories();

            var secondResult = _provider.GetDefaultCategories();

            secondResult.Should().BeSameAs(firstResult);
        }
    }
}
