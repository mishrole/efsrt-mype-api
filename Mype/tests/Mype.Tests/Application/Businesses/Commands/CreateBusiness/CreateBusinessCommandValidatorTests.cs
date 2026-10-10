using System;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using Mype.Application.Businesses.Commands.CreateBusiness;
using Mype.Domain.Businesses.Constraints;
using Mype.Domain.Currencies.Constraints;

namespace Mype.Tests.Application.Businesses.Commands.CreateBusiness
{
    public class CreateBusinessCommandValidatorTests
    {
        private const string ValidDisplayName = "Bodega Central";

        private const string ValidLegalName = "Comercial Central E.I.R.L.";

        private const string ValidRuc = "20123456786";

        private const string ValidCurrencyCode = "PEN";

        private readonly CreateBusinessCommandValidator _validator = new();

        [Fact]
        public async Task Validate_Should_Not_Have_Errors_When_Command_Is_Valid()
        {
            var command = CreateValidCommand();

            var result = await _validator.TestValidateAsync(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public async Task Validate_Should_Not_Have_Errors_When_Optional_Values_Are_Null()
        {
            var command = CreateValidCommand();

            command.LegalName = null;
            command.Ruc = null;

            var result = await _validator.TestValidateAsync(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Validate_Should_Have_Error_When_DisplayName_Is_Empty(string displayName)
        {
            var command = CreateValidCommand();

            command.DisplayName = displayName;

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(current => current.DisplayName);
        }

        [Fact]
        public async Task Validate_Should_Have_Error_When_DisplayName_Exceeds_Maximum_Length()
        {
            var command = CreateValidCommand();

            command.DisplayName = new string('a', BusinessConstraints.DisplayNameMaxLength + 1);

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(current => current.DisplayName);
        }

        [Fact]
        public async Task Validate_Should_Have_Error_When_LegalName_Exceeds_Maximum_Length()
        {
            var command = CreateValidCommand();

            command.LegalName = new string('a', BusinessConstraints.LegalNameMaxLength + 1);

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(current => current.LegalName);
        }

        [Theory]
        [InlineData("2012345678")]
        [InlineData("201234567890")]
        [InlineData("2012345678A")]
        [InlineData("20123456780")]
        public async Task Validate_Should_Have_Error_When_Ruc_Is_Invalid(string ruc)
        {
            var command = CreateValidCommand();

            command.Ruc = ruc;

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(current => current.Ruc);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Validate_Should_Have_Error_When_CurrencyCode_Is_Empty(string currencyCode)
        {
            var command = CreateValidCommand();

            command.CurrencyCode = currencyCode;

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(current => current.CurrencyCode);
        }

        [Theory]
        [InlineData("PE")]
        [InlineData("PENN")]
        public async Task Validate_Should_Have_Error_When_CurrencyCode_Length_Is_Invalid(
            string currencyCode
        )
        {
            var command = CreateValidCommand();

            command.CurrencyCode = currencyCode;

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(current => current.CurrencyCode);
        }

        [Fact]
        public async Task Validate_Should_Accept_CurrencyCode_With_Configured_Length()
        {
            var command = CreateValidCommand();

            command.CurrencyCode = new string('A', CurrencyConstraints.CodeMaxLength);

            var result = await _validator.TestValidateAsync(command);

            result.ShouldNotHaveValidationErrorFor(current => current.CurrencyCode);
        }

        [Fact]
        public async Task Validate_Should_Have_Error_When_CurrentUserId_Is_Empty()
        {
            var command = CreateValidCommand();

            command.CurrentUserId = Guid.Empty;

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(current => current.CurrentUserId);
        }

        private static CreateBusinessCommand CreateValidCommand()
        {
            return new CreateBusinessCommand
            {
                DisplayName = ValidDisplayName,
                LegalName = ValidLegalName,
                Ruc = ValidRuc,
                CurrencyCode = ValidCurrencyCode,
                CurrentUserId = Guid.NewGuid(),
            };
        }
    }
}
