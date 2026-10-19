using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mype.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NameProductCategoryUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_product_categories_business_id_normalized_name",
                table: "product_categories",
                newName: "ux_product_categories_business_normalized_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ux_product_categories_business_normalized_name",
                table: "product_categories",
                newName: "ix_product_categories_business_id_normalized_name");
        }
    }
}
