using FluentValidation.TestHelper;
using Mype.Application.Auth.Commands.RegisterUser;
using System.Threading.Tasks;

namespace Mype.Tests.Application.Auth.Commands.RegisterUser
{
    public class RegisterUserCommandValidatorTests
    {
        private const string ValidDisplayName = "Test User";
        private const string ValidEmail = "user@example.com";
        private const string ValidPassword = "password1";

        private readonly RegisterUserCommandValidator _validator = new();

        [Fact]
        public async Task Validate_Should_Not_Have_Errors_When_Command_Is_Valid()
        {
            var command = CreateValidCommand();

            var result = await _validator.TestValidateAsync(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public async Task Validate_Should_Have_Error_When_DisplayName_Is_Empty()
        {
            var command = CreateValidCommand();
            command.DisplayName = string.Empty;

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(x => x.DisplayName);
        }

        [Fact]
        public async Task Validate_Should_Have_Error_When_DisplayName_Exceeds_Maximum_Length()
        {
            var command = CreateValidCommand();
            command.DisplayName = new string('a', 151);

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(x => x.DisplayName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid-email")]
        [InlineData("@example.com")]
        public async Task Validate_Should_Have_Error_When_Email_Is_Invalid(
        string email
        )
        {
            var command = CreateValidCommand();
            command.Email = email;

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public async Task Validate_Should_Have_Error_When_Email_Exceeds_Maximum_Length()
        {
            var command = CreateValidCommand();
            command.Email = $"{new string('a', 309)}@example.com";

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Theory]
        [InlineData("")]
        [InlineData("pass1")]
        [InlineData("password")]
        [InlineData("12345678")]
        public async Task Validate_Should_Have_Error_When_Password_Is_Invalid(
        string password
        )
        {
            var command = CreateValidCommand();
            command.Password = password;
            command.PasswordConfirmation = password;

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public async Task Validate_Should_Have_Error_When_PasswordConfirmation_Is_Empty()
        {
            var command = CreateValidCommand();
            command.PasswordConfirmation = string.Empty;

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(
            x => x.PasswordConfirmation
            );
        }

        [Fact]
        public async Task Validate_Should_Have_Error_When_Passwords_Do_Not_Match()
        {
            var command = CreateValidCommand();
            command.PasswordConfirmation = "different1";

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(
            x => x.PasswordConfirmation
            );
        }

        private static RegisterUserCommand CreateValidCommand()
        {
            return new RegisterUserCommand
            {
                DisplayName = ValidDisplayName,
                Email = ValidEmail,
                Password = ValidPassword,
                PasswordConfirmation = ValidPassword
            };
        }
    }
}
