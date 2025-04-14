using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeSync.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthTokensTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.RenameTable(
                name: "Tasks",
                newName: "Tasks",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Notes",
                newName: "Notes",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Events",
                newName: "Events",
                newSchema: "public");

            migrationBuilder.CreateTable(
                name: "OAuthTokens",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthTokens", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OAuthTokens",
                schema: "public");

            migrationBuilder.RenameTable(
                name: "Tasks",
                schema: "public",
                newName: "Tasks");

            migrationBuilder.RenameTable(
                name: "Notes",
                schema: "public",
                newName: "Notes");

            migrationBuilder.RenameTable(
                name: "Events",
                schema: "public",
                newName: "Events");
        }
    }
}
