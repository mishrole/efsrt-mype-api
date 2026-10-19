using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mype.Domain.Businesses;
using Mype.Domain.ProductCategories;
using Mype.Domain.Users;
using Mype.Infrastructure.Persistence.Constraints;

namespace Mype.Infrastructure.Persistence.Configurations
{
    public sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
    {
        public void Configure(EntityTypeBuilder<ProductCategory> builder)
        {
            builder.ToTable("product_categories");
            builder.HasKey(category => category.Id);
            builder.Property(category => category.Id).ValueGeneratedNever();
            builder.Property(category => category.Name).HasMaxLength(120).IsRequired();
            builder.Property(category => category.NormalizedName).HasMaxLength(120).IsRequired();
            builder.Property(category => category.Color).HasMaxLength(7).IsRequired();
            builder.Property(category => category.Version).IsRowVersion();
            builder.HasAlternateKey(category => new { category.Id, category.BusinessId });
            builder
                .HasIndex(category => new { category.BusinessId, category.NormalizedName })
                .IsUnique()
                .HasDatabaseName(DatabaseConstraints.ProductCategories.BusinessNormalizedName);
            builder.HasIndex(category => new { category.BusinessId, category.IsActive });
            builder
                .HasOne<Business>()
                .WithMany()
                .HasForeignKey(category => category.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            builder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(category => category.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(category => category.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
