using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mype.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueBusinessRuc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_businesses_ruc",
                table: "businesses",
                column: "ruc",
                unique: true,
                filter: "ruc IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_businesses_ruc",
                table: "businesses");
        }
    }
}
