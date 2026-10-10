using System;
using System.Threading.Tasks;
using FluentAssertions;
using Mype.Application.FinancialMovements.Commands.UpdateSaleItem;

namespace Mype.Tests.Application.FinancialMovements.Commands.UpdateSaleItem
{
    public class UpdateSaleItemCommandValidatorTests
    {
        private readonly UpdateSaleItemCommandValidator _validator = new();

        [Fact]
        public async Task Validate_Should_Accept_Valid_Command() =>
            (await _validator.ValidateAsync(Valid())).IsValid.Should().BeTrue();

        [Fact]
        public async Task Validate_Should_Reject_Empty_Command() =>
            (await _validator.ValidateAsync(new UpdateSaleItemCommand()))
                .IsValid.Should()
                .BeFalse();

        [Theory]
        [InlineData("0", "1")]
        [InlineData("-1", "1")]
        [InlineData("1.12345", "1")]
        [InlineData("1", "-0.01")]
        [InlineData("1", "1.123")]
        public async Task Validate_Should_Reject_Invalid_Amounts(string quantity, string amount)
        {
            var command = Valid();
            command.Quantity = decimal.Parse(
                quantity,
                System.Globalization.CultureInfo.InvariantCulture
            );
            command.UnitAmount = decimal.Parse(
                amount,
                System.Globalization.CultureInfo.InvariantCulture
            );
            (await _validator.ValidateAsync(command)).IsValid.Should().BeFalse();
        }

        private static UpdateSaleItemCommand Valid() =>
            new()
            {
                BusinessId = Guid.NewGuid(),
                MovementId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 1.1234m,
                UnitAmount = 2.22m,
                MovementVersion = 1,
                ItemVersion = 1,
            };
    }
}
