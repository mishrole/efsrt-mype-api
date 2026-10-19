using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mype.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "product_category_id",
                table: "products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "product_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deactivated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_categories", x => x.id);
                    table.UniqueConstraint("ak_product_categories_id_business_id", x => new { x.id, x.business_id });
                    table.ForeignKey(
                        name: "fk_product_categories_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_categories_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_categories_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_products_product_category_id_business_id",
                table: "products",
                columns: new[] { "product_category_id", "business_id" });

            migrationBuilder.CreateIndex(
                name: "ix_product_categories_business_id_is_active",
                table: "product_categories",
                columns: new[] { "business_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_product_categories_business_id_normalized_name",
                table: "product_categories",
                columns: new[] { "business_id", "normalized_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_categories_created_by_user_id",
                table: "product_categories",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_categories_updated_by_user_id",
                table: "product_categories",
                column: "updated_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_products_product_categories_product_category_id_business_id",
                table: "products",
                columns: new[] { "product_category_id", "business_id" },
                principalTable: "product_categories",
                principalColumns: new[] { "id", "business_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_products_product_categories_product_category_id_business_id",
                table: "products");

            migrationBuilder.DropTable(
                name: "product_categories");

            migrationBuilder.DropIndex(
                name: "ix_products_product_category_id_business_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "product_category_id",
                table: "products");
        }
    }
}
