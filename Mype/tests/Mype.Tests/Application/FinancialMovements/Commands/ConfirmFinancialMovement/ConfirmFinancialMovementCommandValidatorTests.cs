using System;
using FluentAssertions;
using FluentValidation.TestHelper;
using Mype.Application.FinancialMovements.Commands.ConfirmFinancialMovement;

namespace Mype.Tests.Application.FinancialMovements.Commands.ConfirmFinancialMovement
{
    public sealed class ConfirmFinancialMovementCommandValidatorTests
    {
        [Fact]
        public void Validate_Should_Accept_Complete_Command()
        {
            var command = new ConfirmFinancialMovementCommand
            {
                BusinessId = Guid.NewGuid(),
                MovementId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                MovementVersion = 1,
                ItemVersions = [new(Guid.NewGuid(), 1)],
            };

            new ConfirmFinancialMovementCommandValidator()
                .TestValidate(command)
                .IsValid.Should()
                .BeTrue();
        }

        [Fact]
        public void Validate_Should_Reject_Empty_Identifiers_Versions_And_Collection()
        {
            var result = new ConfirmFinancialMovementCommandValidator().TestValidate(
                new ConfirmFinancialMovementCommand { ItemVersions = null }
            );

            result.ShouldHaveValidationErrorFor(command => command.BusinessId);
            result.ShouldHaveValidationErrorFor(command => command.MovementId);
            result.ShouldHaveValidationErrorFor(command => command.CurrentUserId);
            result.ShouldHaveValidationErrorFor(command => command.MovementVersion);
            result.ShouldHaveValidationErrorFor(command => command.ItemVersions);
        }

        [Fact]
        public void Validate_Should_Reject_Invalid_And_Duplicate_ItemVersions()
        {
            var itemId = Guid.NewGuid();
            var command = new ConfirmFinancialMovementCommand
            {
                BusinessId = Guid.NewGuid(),
                MovementId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                MovementVersion = 1,
                ItemVersions = [new(Guid.Empty, 0), new(itemId, 1), new(itemId, 2)],
            };

            var result = new ConfirmFinancialMovementCommandValidator().TestValidate(command);

            result.ShouldHaveValidationErrorFor("ItemVersions[0].ItemId");
            result.ShouldHaveValidationErrorFor("ItemVersions[0].Version");
            result.ShouldHaveValidationErrorFor(command => command.ItemVersions);
        }
    }
}
