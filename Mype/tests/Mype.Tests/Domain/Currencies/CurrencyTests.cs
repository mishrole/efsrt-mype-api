using System;
using FluentAssertions;
using Mype.Domain.Currencies;

namespace Mype.Tests.Domain.Currencies
{
    public class CurrencyTests
    {
        [Fact]
        public void CreateSystem_Should_Initialize_Active_Currency()
        {
            var id = Guid.NewGuid();

            var currency = Currency.CreateSystem(id, "PEN", "Sol peruano", "S/", 2);

            currency.Id.Should().Be(id);
            currency.Code.Should().Be("PEN");
            currency.Name.Should().Be("Sol peruano");
            currency.Symbol.Should().Be("S/");
            currency.DecimalPlaces.Should().Be(2);
            currency.IsActive.Should().BeTrue();
        }
    }
}
