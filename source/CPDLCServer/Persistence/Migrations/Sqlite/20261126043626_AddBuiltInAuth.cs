using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPDLCServer.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddBuiltInAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LastUsed",
                table: "PluginTokens",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Administrators",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUsed",
                table: "PluginTokens");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Administrators");
        }
    }
}
