using System;
using Mype.Domain.Common;

namespace Mype.Domain.Currencies
{
    public class Currency : ReferenceEntity
    {
        private Currency() { }

        private Currency(
            Guid id,
            string code,
            string name,
            string symbol,
            short decimalPlaces,
            bool isActive
        )
            : base(id, code, name, isActive)
        {
            Symbol = symbol;
            DecimalPlaces = decimalPlaces;
        }

        public string Symbol { get; private set; } = string.Empty;

        public short DecimalPlaces { get; private set; }

        public static Currency CreateSystem(
            Guid id,
            string code,
            string name,
            string symbol,
            short decimalPlaces
        )
        {
            return new Currency(id, code, name, symbol, decimalPlaces, true);
        }
    }
}
