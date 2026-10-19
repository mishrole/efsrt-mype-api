using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

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

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "id", "code", "created_at", "description", "is_active", "is_system", "name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000025"), "PRODUCT_CATEGORY_READ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite consultar las categorías de producto del negocio.", true, true, "Consultar categorías de producto", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("30000000-0000-0000-0000-000000000026"), "PRODUCT_CATEGORY_CREATE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite crear categorías de producto para el negocio.", true, true, "Crear categorías de producto", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("30000000-0000-0000-0000-000000000027"), "PRODUCT_CATEGORY_UPDATE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite actualizar las categorías de producto del negocio.", true, true, "Actualizar categorías de producto", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("30000000-0000-0000-0000-000000000028"), "PRODUCT_CATEGORY_DEACTIVATE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite desactivar las categorías de producto del negocio.", true, true, "Desactivar categorías de producto", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("30000000-0000-0000-0000-000000000029"), "PRODUCT_CATEGORY_REACTIVATE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite reactivar las categorías de producto del negocio.", true, true, "Reactivar categorías de producto", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "business_role_permissions",
                columns: new[] { "business_role_id", "permission_id", "created_at" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000025"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000026"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000027"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000028"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000029"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
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
                name: "ix_product_categories_created_by_user_id",
                table: "product_categories",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_categories_updated_by_user_id",
                table: "product_categories",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_product_categories_business_normalized_name",
                table: "product_categories",
                columns: new[] { "business_id", "normalized_name" },
                unique: true);

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

            migrationBuilder.DeleteData(
                table: "business_role_permissions",
                keyColumns: new[] { "business_role_id", "permission_id" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000025") });

            migrationBuilder.DeleteData(
                table: "business_role_permissions",
                keyColumns: new[] { "business_role_id", "permission_id" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000026") });

            migrationBuilder.DeleteData(
                table: "business_role_permissions",
                keyColumns: new[] { "business_role_id", "permission_id" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000027") });

            migrationBuilder.DeleteData(
                table: "business_role_permissions",
                keyColumns: new[] { "business_role_id", "permission_id" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000028") });

            migrationBuilder.DeleteData(
                table: "business_role_permissions",
                keyColumns: new[] { "business_role_id", "permission_id" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000029") });

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000025"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000026"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000027"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000028"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000029"));

            migrationBuilder.DropColumn(
                name: "product_category_id",
                table: "products");
        }
    }
}
