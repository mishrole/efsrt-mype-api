using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mype.Domain.Currencies;
using Mype.Domain.Currencies.Constants;
using Mype.Domain.Currencies.Constraints;
using Mype.Infrastructure.Persistence.Constraints;

namespace Mype.Infrastructure.Persistence.Configurations
{
    public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
    {
        public void Configure(EntityTypeBuilder<Currency> builder)
        {
            builder.ToTable("currencies");

            builder.HasKey(currency => currency.Id);

            builder.Property(currency => currency.Id).ValueGeneratedNever();

            builder
                .Property(currency => currency.Code)
                .HasMaxLength(CurrencyConstraints.CodeMaxLength)
                .IsRequired();

            builder
                .HasIndex(currency => currency.Code)
                .IsUnique()
                .HasDatabaseName(DatabaseConstraints.Currencies.Code);

            builder
                .Property(currency => currency.Name)
                .HasMaxLength(CurrencyConstraints.NameMaxLength)
                .IsRequired();

            builder
                .Property(currency => currency.Symbol)
                .HasMaxLength(CurrencyConstraints.SymbolMaxLength)
                .IsRequired();

            builder.Property(currency => currency.DecimalPlaces).IsRequired();

            builder.Property(currency => currency.IsActive).IsRequired();

            builder.HasData(
                new
                {
                    Id = CurrencyConstants.PenId,
                    Code = CurrencyConstants.PenCode,
                    Name = "Sol peruano",
                    Symbol = "S/",
                    DecimalPlaces = (short)2,
                    IsActive = true,
                }
            );
        }
    }
}
