namespace Mype.Domain.FinancialMovements.Constraints
{
    public static class FinancialMovementConstraints
    {
        public const int DescriptionMaxLength = 500;
        public const int CurrencyCodeLength = 3;
        public const int AmountPrecision = 18;
        public const int AmountScale = 2;
        public const int CancellationReasonMaxLength = 500;
    }
}
