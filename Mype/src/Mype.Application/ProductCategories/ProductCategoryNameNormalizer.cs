using System;
using System.Linq;

namespace Mype.Application.ProductCategories
{
    public static class ProductCategoryNameNormalizer
    {
        public static string NormalizeName(string value) =>
            string.Join(
                " ",
                value.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
            );

        public static string NormalizeForComparison(string value) =>
            NormalizeName(value).ToUpperInvariant();
    }
}
