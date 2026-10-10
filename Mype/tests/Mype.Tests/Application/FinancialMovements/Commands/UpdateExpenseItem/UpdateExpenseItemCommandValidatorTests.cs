using System;
using System.Globalization;
using System.Threading.Tasks;
using FluentAssertions;
using Mype.Application.FinancialMovements.Commands.UpdateExpenseItem;
using Mype.Domain.FinancialMovements.Constraints;

namespace Mype.Tests.Application.FinancialMovements.Commands.UpdateExpenseItem
{
    public sealed class UpdateExpenseItemCommandValidatorTests
    {
        private readonly UpdateExpenseItemCommandValidator _validator = new();

        [Fact]
        public async Task Validate_Should_Accept_Valid_Command() =>
            (await _validator.ValidateAsync(Valid())).IsValid.Should().BeTrue();

        [Fact]
        public async Task Validate_Should_Reject_Empty_Command() =>
            (await _validator.ValidateAsync(new UpdateExpenseItemCommand()))
                .IsValid.Should()
                .BeFalse();

        [Fact]
        public async Task Validate_Should_Reject_Long_Description()
        {
            var command = Valid();
            command.Description = new string(
                'a',
                FinancialMovementItemConstraints.DescriptionMaxLength + 1
            );
            (await _validator.ValidateAsync(command)).IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("0", "1")]
        [InlineData("-1", "1")]
        [InlineData("1.12345", "1")]
        [InlineData("1", "-0.01")]
        [InlineData("1", "1.123")]
        public async Task Validate_Should_Reject_Invalid_Amounts(string quantity, string amount)
        {
            var command = Valid();
            command.Quantity = decimal.Parse(quantity, CultureInfo.InvariantCulture);
            command.UnitAmount = decimal.Parse(amount, CultureInfo.InvariantCulture);
            (await _validator.ValidateAsync(command)).IsValid.Should().BeFalse();
        }

        private static UpdateExpenseItemCommand Valid() =>
            new()
            {
                BusinessId = Guid.NewGuid(),
                MovementId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                Description = "Bolsas",
                Quantity = 1.1234m,
                UnitAmount = 2.22m,
                MovementVersion = 1,
                ItemVersion = 1,
            };
    }
}
