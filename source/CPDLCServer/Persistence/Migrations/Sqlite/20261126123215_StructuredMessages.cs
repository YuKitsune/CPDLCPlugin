using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPDLCServer.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class StructuredMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The old messages are plain text, and cannot be converted to elements, so delete them.
            migrationBuilder.Sql("DELETE FROM \"CpdlcMessages\";");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "CpdlcMessages",
                newName: "Elements");

            migrationBuilder.AddColumn<bool>(
                name: "Actioned",
                table: "CpdlcMessages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanAction",
                table: "CpdlcMessages",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Actioned",
                table: "CpdlcMessages");

            migrationBuilder.DropColumn(
                name: "CanAction",
                table: "CpdlcMessages");

            migrationBuilder.RenameColumn(
                name: "Elements",
                table: "CpdlcMessages",
                newName: "Content");
        }
    }
}
