using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Mype.Application.FinancialMovements.Commands.CreateFinancialMovement;
using Mype.Domain.FinancialMovements;
using Mype.Domain.FinancialMovements.Constraints;

namespace Mype.Tests.Application.FinancialMovements.Commands.CreateFinancialMovement
{
    public class CreateFinancialMovementCommandValidatorTests
    {
        private readonly CreateFinancialMovementCommandValidator _validator = new();

        [Theory]
        [InlineData(FinancialMovementType.Sale)]
        [InlineData(FinancialMovementType.Expense)]
        public async Task Validate_Should_Accept_Supported_Type(FinancialMovementType type)
        {
            var command = Valid();
            command.Type = type;
            (await _validator.ValidateAsync(command)).IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task Validate_Should_Reject_Empty_Ids_Invalid_Type_And_Date()
        {
            var command = new CreateFinancialMovementCommand { Type = (FinancialMovementType)99 };
            var result = await _validator.ValidateAsync(command);
            result
                .Errors.Select(error => error.PropertyName)
                .Should()
                .Contain(
                    new[]
                    {
                        nameof(command.BusinessId),
                        nameof(command.CurrentUserId),
                        nameof(command.Type),
                        nameof(command.MovementDate),
                    }
                );
        }

        [Fact]
        public async Task Validate_Should_Reject_Description_Over_Maximum_Length()
        {
            var command = Valid();
            command.Description = new string(
                'x',
                FinancialMovementConstraints.DescriptionMaxLength + 1
            );
            (await _validator.ValidateAsync(command))
                .Errors.Should()
                .Contain(error => error.PropertyName == nameof(command.Description));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task Validate_Should_Allow_Optional_Description(string description)
        {
            var command = Valid();
            command.Description = description;
            (await _validator.ValidateAsync(command)).IsValid.Should().BeTrue();
        }

        private static CreateFinancialMovementCommand Valid() =>
            new()
            {
                BusinessId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                Type = FinancialMovementType.Sale,
                MovementDate = new DateOnly(2026, 10, 10),
            };
    }
}
