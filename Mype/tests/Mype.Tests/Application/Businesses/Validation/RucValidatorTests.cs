using FluentAssertions;
using Mype.Application.Businesses.Validation;

namespace Mype.Tests.Application.Businesses.Validation
{
    public class RucValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValid_Should_Return_False_When_Ruc_Is_Empty(
            string ruc
        )
        {
            var result = RucValidator.IsValid(ruc);

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("2012345678")]
        [InlineData("201234567890")]
        public void IsValid_Should_Return_False_When_Length_Is_Invalid(
            string ruc
        )
        {
            var result = RucValidator.IsValid(ruc);

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("2012345678A")]
        [InlineData("20-23456786")]
        [InlineData("ABCDEFGHIJK")]
        public void IsValid_Should_Return_False_When_Ruc_Contains_Non_Numeric_Characters(
            string ruc
        )
        {
            var result = RucValidator.IsValid(ruc);

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("20123456780")]
        [InlineData("20123456781")]
        [InlineData("20123456782")]
        public void IsValid_Should_Return_False_When_Check_Digit_Is_Invalid(
            string ruc
        )
        {
            var result = RucValidator.IsValid(ruc);

            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_Should_Return_True_When_Ruc_Is_Structurally_Valid()
        {
            var result = RucValidator.IsValid(
                "20123456786"
            );

            result.Should().BeTrue();
        }
    }
}