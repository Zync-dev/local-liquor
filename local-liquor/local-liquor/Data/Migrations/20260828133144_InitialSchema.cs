using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace local_liquor.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastSignedInAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TitleDa = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    TitleEn = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Place = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    StartsOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Hours = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    IsPublished = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    OriginalName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    ByteSize = table.Column<long>(type: "INTEGER", nullable: false),
                    AltDa = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AltEn = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Usage = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Wines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    LabelName = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    NameDa = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    NameEn = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    TaglineDa = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    TaglineEn = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    BodyDa = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: false),
                    BodyEn = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: false),
                    ServingDa = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    ServingEn = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    LiquidColor = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    TintColor = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    AlcoholByVolume = table.Column<string>(type: "TEXT", nullable: false),
                    VolumeMl = table.Column<int>(type: "INTEGER", nullable: false),
                    HarvestMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    BatchSize = table.Column<int>(type: "INTEGER", nullable: false),
                    Stock = table.Column<int>(type: "INTEGER", nullable: false),
                    BottlesLeft = table.Column<int>(type: "INTEGER", nullable: true),
                    IsPublished = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsHero = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WineNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WineId = table.Column<int>(type: "INTEGER", nullable: false),
                    TextDa = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    TextEn = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WineNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WineNotes_Wines_WineId",
                        column: x => x.WineId,
                        principalTable: "Wines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketEvents_StartsOn",
                table: "MarketEvents",
                column: "StartsOn");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_Usage",
                table: "MediaAssets",
                column: "Usage");

            migrationBuilder.CreateIndex(
                name: "IX_WineNotes_WineId",
                table: "WineNotes",
                column: "WineId");

            migrationBuilder.CreateIndex(
                name: "IX_Wines_Slug",
                table: "Wines",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropTable(
                name: "MarketEvents");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropTable(
                name: "WineNotes");

            migrationBuilder.DropTable(
                name: "Wines");
        }
    }
}
