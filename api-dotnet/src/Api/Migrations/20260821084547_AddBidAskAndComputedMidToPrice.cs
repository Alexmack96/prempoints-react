using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBidAskAndComputedMidToPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prices_TeamId",
                table: "Prices");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Prices");

            migrationBuilder.AddColumn<decimal>(
                name: "Ask",
                table: "Prices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Bid",
                table: "Prices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Mid",
                table: "Prices",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                computedColumnSql: "(([Bid] + [Ask]) / 2.0)",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prices_TeamId_ValueDate",
                table: "Prices",
                columns: new[] { "TeamId", "ValueDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prices_TeamId_ValueDate",
                table: "Prices");

            migrationBuilder.DropColumn(
                name: "Mid",
                table: "Prices");

            migrationBuilder.DropColumn(
                name: "Ask",
                table: "Prices");

            migrationBuilder.DropColumn(
                name: "Bid",
                table: "Prices");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Prices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Prices_TeamId",
                table: "Prices",
                column: "TeamId");
        }
    }
}
