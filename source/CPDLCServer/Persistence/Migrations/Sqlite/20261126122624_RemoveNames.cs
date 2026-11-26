using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPDLCServer.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RemoveNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "PluginTokens");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Administrators");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "PluginTokens",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Administrators",
                type: "TEXT",
                nullable: true);
        }
    }
}
