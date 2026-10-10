using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Mype.Domain.FinancialMovements;
using Mype.Infrastructure.Persistence;
using Mype.Infrastructure.Persistence.Exceptions;

namespace Mype.Tests.Infrastructure.Persistence
{
    public class FinancialMovementItemModelTests
    {
        [Fact]
        public void Model_Should_Configure_Item_Table_Relationships_Indexes_And_Concurrency()
        {
            var options = new DbContextOptionsBuilder<MypeDbContext>()
                .UseNpgsql("Host=localhost;Database=model;Username=test;Password=test")
                .Options;
            using var context = new MypeDbContext(
                options,
                new Mock<IPersistenceExceptionTranslator>().Object
            );
            var entity = context.Model.FindEntityType(typeof(FinancialMovementItem));
            entity.Should().NotBeNull();
            entity.GetTableName().Should().Be("financial_movement_items");
            entity
                .FindProperty(nameof(FinancialMovementItem.Quantity))
                .GetPrecision()
                .Should()
                .Be(18);
            entity.FindProperty(nameof(FinancialMovementItem.Quantity)).GetScale().Should().Be(4);
            entity.FindProperty(nameof(FinancialMovementItem.UnitAmount)).GetScale().Should().Be(2);
            entity
                .FindProperty(nameof(FinancialMovementItem.Version))
                .IsConcurrencyToken.Should()
                .BeTrue();
            entity
                .GetIndexes()
                .Should()
                .Contain(index =>
                    index
                        .Properties.Select(p => p.Name)
                        .SequenceEqual(
                            new[]
                            {
                                nameof(FinancialMovementItem.MovementId),
                                nameof(FinancialMovementItem.IsActive),
                            }
                        )
                );
            entity
                .GetForeignKeys()
                .Should()
                .Contain(fk => fk.PrincipalEntityType.ClrType == typeof(FinancialMovement));
        }
    }
}
