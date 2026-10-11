using System;
using FluentAssertions;
using FluentValidation.TestHelper;
using Mype.Application.FinancialMovements.Commands.DiscardFinancialMovement;

namespace Mype.Tests.Application.FinancialMovements.Commands.DiscardFinancialMovement
{
    public sealed class DiscardFinancialMovementCommandValidatorTests
    {
        [Fact]
        public void Validate_Should_Accept_Complete_Command_Without_Evidences()
        {
            var command = new DiscardFinancialMovementCommand
            {
                BusinessId = Guid.NewGuid(),
                MovementId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                MovementVersion = 1,
                EvidenceVersions = [],
            };

            new DiscardFinancialMovementCommandValidator()
                .TestValidate(command)
                .IsValid.Should()
                .BeTrue();
        }

        [Fact]
        public void Validate_Should_Reject_Empty_Fields_And_Null_EvidenceVersions()
        {
            var result = new DiscardFinancialMovementCommandValidator().TestValidate(
                new DiscardFinancialMovementCommand()
            );

            result.ShouldHaveValidationErrorFor(command => command.BusinessId);
            result.ShouldHaveValidationErrorFor(command => command.MovementId);
            result.ShouldHaveValidationErrorFor(command => command.CurrentUserId);
            result.ShouldHaveValidationErrorFor(command => command.MovementVersion);
            result.ShouldHaveValidationErrorFor(command => command.EvidenceVersions);
        }

        [Fact]
        public void Validate_Should_Reject_Invalid_And_Duplicate_EvidenceVersions()
        {
            var evidenceId = Guid.NewGuid();
            var command = new DiscardFinancialMovementCommand
            {
                BusinessId = Guid.NewGuid(),
                MovementId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                MovementVersion = 1,
                EvidenceVersions = [new(Guid.Empty, 0), new(evidenceId, 1), new(evidenceId, 2)],
            };

            var result = new DiscardFinancialMovementCommandValidator().TestValidate(command);

            result.ShouldHaveValidationErrorFor("EvidenceVersions[0].EvidenceId");
            result.ShouldHaveValidationErrorFor("EvidenceVersions[0].Version");
            result.ShouldHaveValidationErrorFor(command => command.EvidenceVersions);
        }
    }
}
