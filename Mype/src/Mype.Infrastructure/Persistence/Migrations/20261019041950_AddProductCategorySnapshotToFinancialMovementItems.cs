using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mype.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCategorySnapshotToFinancialMovementItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "product_category_id",
                table: "financial_movement_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_financial_movement_items_business_product_category",
                table: "financial_movement_items",
                columns: new[] { "business_id", "product_category_id" },
                filter: "product_category_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movement_items_product_category_id_business_id",
                table: "financial_movement_items",
                columns: new[] { "product_category_id", "business_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_financial_movement_items_product_category_snapshot_consiste~",
                table: "financial_movement_items",
                sql: "(\n    product_id IS NOT NULL\n    AND product_category_id IS NOT NULL\n    AND unit_cost_snapshot IS NOT NULL\n)\nOR\n(\n    product_id IS NULL\n    AND product_category_id IS NULL\n    AND unit_cost_snapshot IS NULL\n)");

            migrationBuilder.AddForeignKey(
                name: "fk_financial_movement_items_product_categories_product_category_id_business_id",
                table: "financial_movement_items",
                columns: new[] { "product_category_id", "business_id" },
                principalTable: "product_categories",
                principalColumns: new[] { "id", "business_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_financial_movement_items_product_categories_product_category_id_business_id",
                table: "financial_movement_items");

            migrationBuilder.DropIndex(
                name: "ix_financial_movement_items_business_product_category",
                table: "financial_movement_items");

            migrationBuilder.DropIndex(
                name: "ix_financial_movement_items_product_category_id_business_id",
                table: "financial_movement_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_financial_movement_items_product_category_snapshot_consiste~",
                table: "financial_movement_items");

            migrationBuilder.DropColumn(
                name: "product_category_id",
                table: "financial_movement_items");
        }
    }
}
