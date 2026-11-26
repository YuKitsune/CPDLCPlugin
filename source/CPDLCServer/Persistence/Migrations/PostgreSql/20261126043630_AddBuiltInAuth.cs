using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPDLCServer.Persistence.Migrations.PostgreSql
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
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Administrators",
                type: "text",
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
