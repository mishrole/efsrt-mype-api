using System;
using System.Threading.Tasks;
using FluentAssertions;
using Mype.Application.FinancialMovements.Commands.RetireFinancialMovementItem;

namespace Mype.Tests.Application.FinancialMovements.Commands.RetireFinancialMovementItem
{
    public class RetireFinancialMovementItemCommandValidatorTests
    {
        private readonly RetireFinancialMovementItemCommandValidator _validator = new();

        [Fact]
        public async Task Validate_Should_Accept_Valid_Command() =>
            (await _validator.ValidateAsync(Valid())).IsValid.Should().BeTrue();

        [Fact]
        public async Task Validate_Should_Reject_Empty_Command() =>
            (await _validator.ValidateAsync(new RetireFinancialMovementItemCommand()))
                .IsValid.Should()
                .BeFalse();

        private static RetireFinancialMovementItemCommand Valid() =>
            new()
            {
                BusinessId = Guid.NewGuid(),
                MovementId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                MovementVersion = 1,
                ItemVersion = 1,
            };
    }
}
