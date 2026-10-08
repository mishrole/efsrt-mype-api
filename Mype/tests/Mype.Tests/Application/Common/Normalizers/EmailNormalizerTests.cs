using FluentAssertions;
using Mype.Application.Common.Normalizers;
using System;

namespace Mype.Tests.Application.Common.Normalizers
{
    public class EmailNormalizerTests
    {
        private readonly EmailNormalizer _normalizer = new();

        [Theory]
        [InlineData("user@example.com", "USER@EXAMPLE.COM")]
        [InlineData("USER@EXAMPLE.COM", "USER@EXAMPLE.COM")]
        [InlineData(" user@example.com ", "USER@EXAMPLE.COM")]
        public void Normalize_Should_Trim_And_Convert_To_Uppercase(
            string email,
            string expected
        )
        {
            var result = _normalizer.Normalize(email);

            result.Should().Be(expected);
        }

        [Fact]
        public void Normalize_Should_Throw_When_Email_Is_Null()
        {
            var action = () => _normalizer.Normalize(null);

            action.Should().Throw<ArgumentNullException>();
        }
    }
}
