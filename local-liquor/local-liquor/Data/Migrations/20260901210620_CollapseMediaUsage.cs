using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace local_liquor.Data.Migrations
{
    /// <inheritdoc />
    public partial class CollapseMediaUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MediaUsage lost its Craft member: the front page has one photo strip
            // now, not a story section and a craft section. Anything tagged Craft
            // moves to the strip that survived rather than falling out of view.
            migrationBuilder.Sql("UPDATE MediaAssets SET Usage = 1 WHERE Usage = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Which photos were Craft is not recoverable; they stay on the strip.
        }
    }
}
