using System;
using FluentAssertions;
using FluentValidation.TestHelper;
using Mype.Application.Businesses.Commands.ReplaceOpeningBalance;
using Mype.Domain.FinancialMovements.Constraints;

namespace Mype.Tests.Application.Businesses.Commands.ReplaceOpeningBalance
{
    public sealed class ReplaceOpeningBalanceCommandValidatorTests
    {
        private readonly ReplaceOpeningBalanceCommandValidator _validator = new();

        [Fact]
        public void Validate_Should_Accept_Positive_Zero_And_Negative_Amounts()
        {
            foreach (var amount in new[] { 1000m, 0m, -500m })
                _validator.TestValidate(ValidCommand(amount)).IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_Should_Reject_Empty_Identifiers_Version_And_Reason()
        {
            var result = _validator.TestValidate(new ReplaceOpeningBalanceCommand());
            result.ShouldHaveValidationErrorFor(x => x.BusinessId);
            result.ShouldHaveValidationErrorFor(x => x.CurrentUserId);
            result.ShouldHaveValidationErrorFor(x => x.MovementVersion);
            result.ShouldHaveValidationErrorFor(x => x.Reason);
        }

        [Fact]
        public void Validate_Should_Use_Configured_Reason_Limits()
        {
            _validator
                .TestValidate(
                    ValidCommand(
                        0m,
                        new string(
                            'a',
                            FinancialMovementConstraints.OpeningBalanceReplacementReasonMinLength
                                - 1
                        )
                    )
                )
                .ShouldHaveValidationErrorFor(x => x.Reason);
            _validator
                .TestValidate(
                    ValidCommand(
                        0m,
                        new string(
                            'a',
                            FinancialMovementConstraints.OpeningBalanceReplacementReasonMinLength
                        )
                    )
                )
                .ShouldNotHaveValidationErrorFor(x => x.Reason);
            _validator
                .TestValidate(
                    ValidCommand(
                        0m,
                        new string(
                            'a',
                            FinancialMovementConstraints.OpeningBalanceReplacementReasonMaxLength
                        )
                    )
                )
                .ShouldNotHaveValidationErrorFor(x => x.Reason);
            _validator
                .TestValidate(
                    ValidCommand(
                        0m,
                        new string(
                            'a',
                            FinancialMovementConstraints.OpeningBalanceReplacementReasonMaxLength
                                + 1
                        )
                    )
                )
                .ShouldHaveValidationErrorFor(x => x.Reason);
        }

        private static ReplaceOpeningBalanceCommand ValidCommand(
            decimal amount,
            string reason = "Conciliación inicial"
        ) =>
            new()
            {
                BusinessId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                Amount = amount,
                Reason = reason,
                MovementVersion = 4,
            };
    }
}
