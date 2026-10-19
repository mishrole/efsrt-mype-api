using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Mype.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCategoryPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
