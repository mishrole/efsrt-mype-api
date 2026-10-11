using System;
using FluentAssertions;
using FluentValidation.TestHelper;
using Mype.Application.FinancialMovements.Commands.CancelFinancialMovement;
using Mype.Domain.FinancialMovements.Constraints;

namespace Mype.Tests.Application.FinancialMovements.Commands.CancelFinancialMovement
{
    public sealed class CancelFinancialMovementCommandValidatorTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Validate_Should_Accept_Exact_Reason_Limits(bool minimum)
        {
            var length = minimum
                ? FinancialMovementConstraints.CancellationReasonMinLength
                : FinancialMovementConstraints.CancellationReasonMaxLength;
            var command = ValidCommand(new string('a', length));

            new CancelFinancialMovementCommandValidator()
                .TestValidate(command)
                .IsValid.Should()
                .BeTrue();
        }

        [Fact]
        public void Validate_Should_Use_Normalized_Reason_Length()
        {
            var significantLength = FinancialMovementConstraints.CancellationReasonMinLength;
            var command = ValidCommand(
                $"  {new string('a', significantLength / 2)}     {new string('b', significantLength - significantLength / 2 - 1)}  "
            );

            new CancelFinancialMovementCommandValidator()
                .TestValidate(command)
                .IsValid.Should()
                .BeTrue();
        }

        [Fact]
        public void Validate_Should_Reject_Empty_Identifiers_Version_And_Reason()
        {
            var result = new CancelFinancialMovementCommandValidator().TestValidate(
                new CancelFinancialMovementCommand { Reason = "   " }
            );

            result.ShouldHaveValidationErrorFor(command => command.BusinessId);
            result.ShouldHaveValidationErrorFor(command => command.MovementId);
            result.ShouldHaveValidationErrorFor(command => command.CurrentUserId);
            result.ShouldHaveValidationErrorFor(command => command.MovementVersion);
            result.ShouldHaveValidationErrorFor(command => command.Reason);
        }

        [Fact]
        public void Validate_Should_Reject_Reason_Outside_Configured_Limits()
        {
            var minimumResult = new CancelFinancialMovementCommandValidator().TestValidate(
                ValidCommand(
                    new string('a', FinancialMovementConstraints.CancellationReasonMinLength - 1)
                )
            );
            var maximumResult = new CancelFinancialMovementCommandValidator().TestValidate(
                ValidCommand(
                    new string('a', FinancialMovementConstraints.CancellationReasonMaxLength + 1)
                )
            );

            minimumResult.ShouldHaveValidationErrorFor(command => command.Reason);
            maximumResult.ShouldHaveValidationErrorFor(command => command.Reason);
        }

        private static CancelFinancialMovementCommand ValidCommand(string reason) =>
            new()
            {
                BusinessId = Guid.NewGuid(),
                MovementId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                MovementVersion = 1,
                Reason = reason,
            };
    }
}
