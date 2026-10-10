using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mype.Domain.Businesses;
using Mype.Domain.Businesses.Constraints;
using Mype.Domain.Currencies;
using Mype.Domain.Users;
using Mype.Infrastructure.Persistence.Constraints;

namespace Mype.Infrastructure.Persistence.Configurations
{
    public class BusinessConfiguration : IEntityTypeConfiguration<Business>
    {
        public void Configure(EntityTypeBuilder<Business> builder)
        {
            builder.ToTable("businesses");

            builder.HasKey(business => business.Id);

            builder.Property(business => business.Id).ValueGeneratedNever();

            builder
                .Property(business => business.DisplayName)
                .HasMaxLength(BusinessConstraints.DisplayNameMaxLength)
                .IsRequired();

            builder
                .Property(business => business.LegalName)
                .HasMaxLength(BusinessConstraints.LegalNameMaxLength);

            builder.Property(business => business.Ruc).HasMaxLength(BusinessConstraints.RucLength);

            builder
                .HasIndex(x => x.Ruc)
                .IsUnique()
                .HasFilter("ruc IS NOT NULL")
                .HasDatabaseName(DatabaseConstraints.Businesses.Ruc);

            builder.Property(business => business.CurrencyId).IsRequired();

            builder
                .HasOne<Currency>()
                .WithMany()
                .HasForeignKey(business => business.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.Businesses.Currency);

            builder.Property(business => business.Status).IsRequired();

            builder.Property(business => business.CreatedByUserId).IsRequired();

            builder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(business => business.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.Businesses.CreatedByUser);

            builder.Property(business => business.UpdatedByUserId).IsRequired();

            builder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(business => business.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.Businesses.UpdatedByUser);

            builder
                .Property(business => business.DeactivatedAt)
                .HasColumnType("timestamp with time zone");

            builder
                .Property(business => business.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder
                .Property(business => business.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(business => business.Version).IsRowVersion();
        }
    }
}
