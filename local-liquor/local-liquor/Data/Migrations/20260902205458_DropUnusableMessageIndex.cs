using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace local_liquor.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropUnusableMessageIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContactMessages_ReceivedAt",
                table: "ContactMessages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_ReceivedAt",
                table: "ContactMessages",
                column: "ReceivedAt");
        }
    }
}
