namespace Mype.Domain.ProductCategories.Constraints
{
    public static class ProductCategoryConstraints
    {
        public const int NameMaxLength = 120;
        public const int NormalizedNameMaxLength = 120;
        public const int ColorLength = 7;
        public const string ColorPattern = "^#[0-9A-Fa-f]{6}$";
    }
}
