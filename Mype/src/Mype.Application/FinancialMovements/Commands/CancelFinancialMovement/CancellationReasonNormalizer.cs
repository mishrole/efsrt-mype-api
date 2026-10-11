using System.Text.RegularExpressions;

namespace Mype.Application.FinancialMovements.Commands.CancelFinancialMovement
{
    public static class CancellationReasonNormalizer
    {
        private static readonly Regex Whitespace = new("\\s+", RegexOptions.Compiled);

        public static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : Whitespace.Replace(value.Trim(), " ");
    }
}
