using System.Threading.Tasks;
using FluentValidation.TestHelper;
using Mype.Application.Auth.Commands.Login;
using Mype.Domain.Users.Constraints;

namespace Mype.Tests.Application.Auth.Commands.Login
{
    public class LoginCommandValidatorTests
    {
        private const string ValidEmail = "user@example.com";
        private const string ValidPassword = "password1";

        private readonly LoginCommandValidator _validator = new();

        [Fact]
        public async Task Validate_Should_Not_Have_Errors_When_Command_Is_Valid()
        {
            var command = CreateValidCommand();

            var result = await _validator.TestValidateAsync(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public async Task Validate_Should_Have_Error_When_Email_Is_Empty()
        {
            var command = CreateValidCommand();
            command.Email = string.Empty;

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(current => current.Email);
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("@example.com")]
        [InlineData("user@")]
        public async Task Validate_Should_Have_Error_When_Email_Is_Invalid(string email)
        {
            var command = CreateValidCommand();
            command.Email = email;

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(current => current.Email);
        }

        [Fact]
        public async Task Validate_Should_Have_Error_When_Email_Exceeds_Maximum_Length()
        {
            var command = CreateValidCommand();

            command.Email = new string('a', UserConstraints.EmailMaxLength + 1);

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(current => current.Email);
        }

        [Fact]
        public async Task Validate_Should_Have_Error_When_Password_Is_Empty()
        {
            var command = CreateValidCommand();
            command.Password = string.Empty;

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(current => current.Password);
        }

        [Fact]
        public async Task Validate_Should_Not_Apply_Registration_Password_Policy()
        {
            var command = CreateValidCommand();
            command.Password = "a";

            var result = await _validator.TestValidateAsync(command);

            result.ShouldNotHaveValidationErrorFor(current => current.Password);
        }

        private static LoginCommand CreateValidCommand()
        {
            return new LoginCommand { Email = ValidEmail, Password = ValidPassword };
        }
    }
}
