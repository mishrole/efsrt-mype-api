using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Mype.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "business_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_business_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "currencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    decimal_places = table.Column<short>(type: "smallint", nullable: false),
                    code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_currencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "businesses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ruc = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deactivated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_businesses", x => x.id);
                    table.ForeignKey(
                        name: "fk_businesses_currencies_currency_id",
                        column: x => x.currency_id,
                        principalTable: "currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_businesses_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_businesses_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "business_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deactivated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reactivated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_business_memberships", x => x.id);
                    table.ForeignKey(
                        name: "fk_business_memberships_business_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "business_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_business_memberships_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_business_memberships_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_business_memberships_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_business_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
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
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_categories_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_categories_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_categories_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "business_roles",
                columns: new[] { "id", "code", "created_at", "description", "is_active", "is_system", "name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), "OWNER", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Administra el negocio y sus miembros.", true, true, "Propietario", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "COLLABORATOR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Participa en las operaciones del negocio.", true, true, "Colaborador", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "currencies",
                columns: new[] { "id", "code", "decimal_places", "is_active", "name", "symbol" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000001"), "PEN", (short)2, true, "Sol peruano", "S/" });

            migrationBuilder.CreateIndex(
                name: "ix_business_memberships_business_id_status",
                table: "business_memberships",
                columns: new[] { "business_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_business_memberships_created_by_user_id",
                table: "business_memberships",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_business_memberships_role_id",
                table: "business_memberships",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_business_memberships_updated_by_user_id",
                table: "business_memberships",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_business_memberships_user_id_status",
                table: "business_memberships",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_business_memberships_business_user",
                table: "business_memberships",
                columns: new[] { "business_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_business_roles_code",
                table: "business_roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_businesses_created_by_user_id",
                table: "businesses",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_businesses_currency_id",
                table: "businesses",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "ix_businesses_updated_by_user_id",
                table: "businesses",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_categories_business_id_type_is_active",
                table: "categories",
                columns: new[] { "business_id", "type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_categories_created_by_user_id",
                table: "categories",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_categories_updated_by_user_id",
                table: "categories",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_categories_business_type_name",
                table: "categories",
                columns: new[] { "business_id", "type", "normalized_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_categories_id_business",
                table: "categories",
                columns: new[] { "id", "business_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_currencies_code",
                table: "currencies",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "business_memberships");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "business_roles");

            migrationBuilder.DropTable(
                name: "businesses");

            migrationBuilder.DropTable(
                name: "currencies");
        }
    }
}
