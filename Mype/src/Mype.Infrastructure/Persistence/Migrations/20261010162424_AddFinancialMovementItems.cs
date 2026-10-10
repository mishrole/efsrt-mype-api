using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mype.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialMovementItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "financial_movement_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_cost_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    subtotal_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    retired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_movement_items", x => x.id);
                    table.CheckConstraint("ck_financial_movement_items_quantity_positive", "quantity > 0");
                    table.CheckConstraint("ck_financial_movement_items_subtotal_non_negative", "subtotal_amount >= 0");
                    table.CheckConstraint("ck_financial_movement_items_unit_amount_non_negative", "unit_amount >= 0");
                    table.CheckConstraint("ck_financial_movement_items_unit_cost_non_negative", "unit_cost_snapshot IS NULL OR unit_cost_snapshot >= 0");
                    table.ForeignKey(
                        name: "fk_financial_movement_items_categories_category_id_business_id",
                        columns: x => new { x.category_id, x.business_id },
                        principalTable: "categories",
                        principalColumns: new[] { "id", "business_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_movement_items_movements_movement_id_business_id",
                        columns: x => new { x.movement_id, x.business_id },
                        principalTable: "financial_movements",
                        principalColumns: new[] { "id", "business_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_movement_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_movement_items_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_movement_items_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_financial_movement_items_business_category",
                table: "financial_movement_items",
                columns: new[] { "business_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_movement_items_business_product",
                table: "financial_movement_items",
                columns: new[] { "business_id", "product_id" },
                filter: "product_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movement_items_category_id_business_id",
                table: "financial_movement_items",
                columns: new[] { "category_id", "business_id" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_movement_items_created_by_user_id",
                table: "financial_movement_items",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movement_items_movement_active",
                table: "financial_movement_items",
                columns: new[] { "movement_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_movement_items_movement_id_business_id",
                table: "financial_movement_items",
                columns: new[] { "movement_id", "business_id" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_movement_items_product_id",
                table: "financial_movement_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movement_items_updated_by_user_id",
                table: "financial_movement_items",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financial_movement_items");
        }
    }
}
