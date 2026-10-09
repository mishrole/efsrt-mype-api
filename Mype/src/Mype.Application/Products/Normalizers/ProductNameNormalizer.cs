using System;

namespace Mype.Application.Products.Normalizers
{
    public static class ProductNameNormalizer
    {
        public static string NormalizeName(
            string name
        )
        {
            ArgumentNullException.ThrowIfNull(name);

            return string.Join(
                " ",
                name.Split(
                    ' ',
                    StringSplitOptions
                        .RemoveEmptyEntries
                )
            );
        }

        public static string NormalizeForComparison(
            string name
        )
        {
            return NormalizeName(name)
                .ToUpperInvariant();
        }
    }
}