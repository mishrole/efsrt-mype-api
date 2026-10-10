using FluentAssertions;
using Mype.Application.FinancialMovements.Commands.UpdateFinancialMovementDraft;
using Mype.Domain.FinancialMovements.Constraints;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mype.Tests.Application.FinancialMovements.Commands.UpdateFinancialMovementDraft
{
    public class UpdateFinancialMovementDraftCommandValidatorTests
    {
        private readonly UpdateFinancialMovementDraftCommandValidator _validator = new();

        [Fact]
        public async Task Validate_Should_Accept_Valid_Command() => (await _validator.ValidateAsync(Valid())).IsValid.Should().BeTrue();

        [Fact]
        public async Task Validate_Should_Reject_Empty_Ids_Date_And_Version()
        {
            var command = new UpdateFinancialMovementDraftCommand();
            var result = await _validator.ValidateAsync(command);
            result.Errors.Select(error => error.PropertyName).Should().Contain(new[] { nameof(command.BusinessId), nameof(command.MovementId), nameof(command.CurrentUserId), nameof(command.MovementDate), nameof(command.Version) });
        }

        [Fact]
        public async Task Validate_Should_Reject_Description_Over_Maximum_Length()
        {
            var command = Valid(); command.Description = new string('x', FinancialMovementConstraints.DescriptionMaxLength + 1);
            (await _validator.ValidateAsync(command)).Errors.Should().Contain(error => error.PropertyName == nameof(command.Description));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task Validate_Should_Allow_Clearing_Description(string description)
        {
            var command = Valid(); command.Description = description;
            (await _validator.ValidateAsync(command)).IsValid.Should().BeTrue();
        }

        private static UpdateFinancialMovementDraftCommand Valid() => new() { BusinessId = Guid.NewGuid(), MovementId = Guid.NewGuid(), CurrentUserId = Guid.NewGuid(), MovementDate = new DateOnly(2026, 10, 10), Version = 1 };
    }
}
