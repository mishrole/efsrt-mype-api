using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mype.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMovementEvidences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "movement_evidences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    replaces_evidence_id = table.Column<Guid>(type: "uuid", nullable: true),
                    storage_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    storage_container = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    storage_object_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    removal_reason = table.Column<int>(type: "integer", nullable: true),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    marked_for_purge_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    purge_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    purged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movement_evidences", x => x.id);
                    table.CheckConstraint("ck_movement_evidences_active_state", "status <> 1 OR (removal_reason IS NULL AND marked_for_purge_at IS NULL AND purge_after IS NULL AND purged_at IS NULL)");
                    table.CheckConstraint("ck_movement_evidences_pending_purge_state", "status <> 2 OR (removal_reason IS NOT NULL AND marked_for_purge_at IS NOT NULL AND purge_after IS NOT NULL AND purged_at IS NULL)");
                    table.CheckConstraint("ck_movement_evidences_size_positive", "size_bytes > 0");
                    table.ForeignKey(
                        name: "fk_movement_evidences_movements_movement_id_business_id",
                        columns: x => new { x.movement_id, x.business_id },
                        principalTable: "financial_movements",
                        principalColumns: new[] { "id", "business_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movement_evidences_replaces_evidence_id",
                        column: x => x.replaces_evidence_id,
                        principalTable: "movement_evidences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movement_evidences_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movement_evidences_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movement_evidences_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_movement_evidences_business_status",
                table: "movement_evidences",
                columns: new[] { "business_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_movement_evidences_created_by_user_id",
                table: "movement_evidences",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_movement_evidences_movement_business_status",
                table: "movement_evidences",
                columns: new[] { "movement_id", "business_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_movement_evidences_pending_purge",
                table: "movement_evidences",
                column: "purge_after",
                filter: "status = 2");

            migrationBuilder.CreateIndex(
                name: "ix_movement_evidences_updated_by_user_id",
                table: "movement_evidences",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_movement_evidences_uploaded_by_user_id",
                table: "movement_evidences",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_movement_evidences_replaces_evidence_id",
                table: "movement_evidences",
                column: "replaces_evidence_id",
                unique: true,
                filter: "replaces_evidence_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_movement_evidences_storage_provider_container_object",
                table: "movement_evidences",
                columns: new[] { "storage_provider", "storage_container", "storage_object_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movement_evidences");
        }
    }
}
