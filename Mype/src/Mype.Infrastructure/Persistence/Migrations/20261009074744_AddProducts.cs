using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mype.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_categories_id_business",
                table: "categories");

            migrationBuilder.AddUniqueConstraint(
                name: "ux_categories_id_business",
                table: "categories",
                columns: new[] { "id", "business_id" });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    sale_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("pk_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_categories_category_id_business_id",
                        columns: x => new { x.category_id, x.business_id },
                        principalTable: "categories",
                        principalColumns: new[] { "id", "business_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_products_business_active_name",
                table: "products",
                columns: new[] { "business_id", "is_active", "normalized_name" });

            migrationBuilder.CreateIndex(
                name: "ix_products_business_category_active",
                table: "products",
                columns: new[] { "business_id", "category_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id_business_id",
                table: "products",
                columns: new[] { "category_id", "business_id" });

            migrationBuilder.CreateIndex(
                name: "ix_products_created_by_user_id",
                table: "products",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_updated_by_user_id",
                table: "products",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_products_business_name",
                table: "products",
                columns: new[] { "business_id", "normalized_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropUniqueConstraint(
                name: "ux_categories_id_business",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "ux_categories_id_business",
                table: "categories",
                columns: new[] { "id", "business_id" },
                unique: true);
        }
    }
}
