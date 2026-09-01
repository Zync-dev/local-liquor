using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace local_liquor.Data.Migrations
{
    /// <inheritdoc />
    public partial class BrandRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TintColor",
                table: "Wines",
                newName: "AccentColor");

            migrationBuilder.AddColumn<string>(
                name: "Batch",
                table: "Wines",
                type: "TEXT",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IngredientsDa",
                table: "Wines",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IngredientsEn",
                table: "Wines",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubtitleEn",
                table: "Wines",
                type: "TEXT",
                maxLength: 60,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Batch",
                table: "Wines");

            migrationBuilder.DropColumn(
                name: "IngredientsDa",
                table: "Wines");

            migrationBuilder.DropColumn(
                name: "IngredientsEn",
                table: "Wines");

            migrationBuilder.DropColumn(
                name: "SubtitleEn",
                table: "Wines");

            migrationBuilder.RenameColumn(
                name: "AccentColor",
                table: "Wines",
                newName: "TintColor");
        }
    }
}
