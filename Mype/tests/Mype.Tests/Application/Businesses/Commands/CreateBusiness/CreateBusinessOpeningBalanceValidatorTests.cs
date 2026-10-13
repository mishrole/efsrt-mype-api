using System;
using FluentAssertions;
using FluentValidation.TestHelper;
using Mype.Application.Businesses.Commands.CreateBusiness;

namespace Mype.Tests.Application.Businesses.Commands.CreateBusiness
{
    public sealed class CreateBusinessOpeningBalanceValidatorTests
    {
        private readonly CreateBusinessCommandValidator _validator = new();

        [Theory]
        [InlineData(2500.00)]
        [InlineData(0.00)]
        [InlineData(-500.00)]
        public void Validate_Should_Accept_OpeningBalance_Amounts(decimal amount)
        {
            var result = _validator.TestValidate(CreateValidCommand(amount));
            result.ShouldNotHaveValidationErrorFor(command => command.OpeningBalanceAmount);
            result.ShouldNotHaveValidationErrorFor(command => command.OpeningBalanceDate);
        }

        [Fact]
        public void Validate_Should_Reject_Default_OpeningBalanceDate()
        {
            var command = CreateValidCommand(0m);
            command.OpeningBalanceDate = default;

            _validator
                .TestValidate(command)
                .ShouldHaveValidationErrorFor(current => current.OpeningBalanceDate);
        }

        private static CreateBusinessCommand CreateValidCommand(decimal amount) =>
            new()
            {
                DisplayName = "Bodega Central",
                LegalName = "Comercial Central E.I.R.L.",
                Ruc = "20123456786",
                CurrencyCode = "PEN",
                OpeningBalanceAmount = amount,
                OpeningBalanceDate = new DateOnly(2026, 10, 1),
                CurrentUserId = Guid.NewGuid(),
            };
    }
}
