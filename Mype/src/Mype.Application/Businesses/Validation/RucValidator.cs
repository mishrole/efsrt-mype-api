using System.Linq;

namespace Mype.Application.Businesses.Validation
{
    public static class RucValidator
    {
        private static readonly int[] Weights =
        [
            5,
            4,
            3,
            2,
            7,
            6,
            5,
            4,
            3,
            2
        ];

        public static bool IsValid(string ruc)
        {
            if (
                string.IsNullOrWhiteSpace(ruc) ||
                ruc.Length != 11 ||
                !ruc.All(char.IsDigit)
            )
            {
                return false;
            }

            var sum = 0;

            for (var index = 0; index < Weights.Length; index++)
            {
                sum +=
                    (ruc[index] - '0') *
                    Weights[index];
            }

            var result = 11 - sum % 11;

            var expectedDigit = result switch
            {
                10 => 0,
                11 => 1,
                _ => result
            };

            return expectedDigit == ruc[10] - '0';
        }
    }
}
