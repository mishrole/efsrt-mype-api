using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Mype.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOpeningBalanceMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_current_opening_balance",
                table: "financial_movements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "replacement_reason",
                table: "financial_movements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "replaces_movement_id",
                table: "financial_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "id", "code", "created_at", "description", "is_active", "is_system", "name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000023"), "OPENING_BALANCE_CREATE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite crear el saldo de apertura al crear el negocio.", true, true, "Crear saldo de apertura", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("30000000-0000-0000-0000-000000000024"), "OPENING_BALANCE_REPLACE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite sustituir el saldo de apertura con auditoría.", true, true, "Corregir saldo de apertura", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "business_role_permissions",
                columns: new[] { "business_role_id", "permission_id", "created_at" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000023"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000024"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_business_id",
                table: "financial_movements",
                column: "business_id",
                unique: true,
                filter: "type = 3 AND is_current_opening_balance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_financial_movements_business_id",
                table: "financial_movements");

            migrationBuilder.DeleteData(
                table: "business_role_permissions",
                keyColumns: new[] { "business_role_id", "permission_id" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000023") });

            migrationBuilder.DeleteData(
                table: "business_role_permissions",
                keyColumns: new[] { "business_role_id", "permission_id" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000024") });

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000023"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000024"));

            migrationBuilder.DropColumn(
                name: "is_current_opening_balance",
                table: "financial_movements");

            migrationBuilder.DropColumn(
                name: "replacement_reason",
                table: "financial_movements");

            migrationBuilder.DropColumn(
                name: "replaces_movement_id",
                table: "financial_movements");
        }
    }
}
