using System.Text.RegularExpressions;

namespace Mype.Application.FinancialMovements.Commands.CancelFinancialMovement
{
    public static partial class CancellationReasonNormalizer
    {
        private const int RegexTimeoutMilliseconds = 100;

        public static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : WhitespaceRegex().Replace(value.Trim(), " ");

        [GeneratedRegex(@"\s+", RegexOptions.None, RegexTimeoutMilliseconds)]
        private static partial Regex WhitespaceRegex();
    }
}
