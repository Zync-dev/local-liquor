using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace local_liquor.Data.Migrations
{
    /// <inheritdoc />
    public partial class BatchesAndMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketEvents");

            migrationBuilder.CreateTable(
                name: "Batches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    WineId = table.Column<int>(type: "INTEGER", nullable: true),
                    Stage = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    BottledOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Litres = table.Column<string>(type: "TEXT", nullable: false),
                    FruitKg = table.Column<string>(type: "TEXT", nullable: false),
                    SugarKg = table.Column<string>(type: "TEXT", nullable: false),
                    StartGravity = table.Column<string>(type: "TEXT", nullable: true),
                    EndGravity = table.Column<string>(type: "TEXT", nullable: true),
                    BottleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Batches_Wines_WineId",
                        column: x => x.WineId,
                        principalTable: "Wines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ContactMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BatchSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    DueOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DoneOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchSteps_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Batches_Stage",
                table: "Batches",
                column: "Stage");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_StartedOn",
                table: "Batches",
                column: "StartedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_WineId",
                table: "Batches",
                column: "WineId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchSteps_BatchId",
                table: "BatchSteps",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchSteps_DoneOn_DueOn",
                table: "BatchSteps",
                columns: new[] { "DoneOn", "DueOn" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_IsRead",
                table: "ContactMessages",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_ReceivedAt",
                table: "ContactMessages",
                column: "ReceivedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BatchSteps");

            migrationBuilder.DropTable(
                name: "ContactMessages");

            migrationBuilder.DropTable(
                name: "Batches");

            migrationBuilder.CreateTable(
                name: "MarketEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Hours = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    IsPublished = table.Column<bool>(type: "INTEGER", nullable: false),
                    Place = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TitleDa = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    TitleEn = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketEvents_StartsOn",
                table: "MarketEvents",
                column: "StartsOn");
        }
    }
}
