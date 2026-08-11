using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations;

/// <inheritdoc />
public partial class AddGlobalUniqueConstraints : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.DropIndex(
            name: "IX_Trades_UserId",
            table: "Trades");

        migrationBuilder.DropIndex(
            name: "IX_Seasons_SeasonName_StartYear",
            table: "Seasons");

        migrationBuilder.AlterColumn<string>(
            name: "Username",
            table: "Users",
            type: "nvarchar(450)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AlterColumn<string>(
            name: "Email",
            table: "Users",
            type: "nvarchar(450)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AlterColumn<string>(
            name: "TeamName",
            table: "Teams",
            type: "nvarchar(450)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AlterColumn<string>(
            name: "SeasonName",
            table: "Seasons",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(450)");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_Username",
            table: "Users",
            column: "Username",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Trades_UserId_PriceId",
            table: "Trades",
            columns: ["UserId", "PriceId"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Teams_TeamName",
            table: "Teams",
            column: "TeamName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Seasons_StartYear",
            table: "Seasons",
            column: "StartYear",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SeasonPeriods_PeriodEndDate",
            table: "SeasonPeriods",
            column: "PeriodEndDate",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SeasonPeriods_PeriodStartDate",
            table: "SeasonPeriods",
            column: "PeriodStartDate",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.DropIndex(
            name: "IX_Users_Email",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_Users_Username",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_Trades_UserId_PriceId",
            table: "Trades");

        migrationBuilder.DropIndex(
            name: "IX_Teams_TeamName",
            table: "Teams");

        migrationBuilder.DropIndex(
            name: "IX_Seasons_StartYear",
            table: "Seasons");

        migrationBuilder.DropIndex(
            name: "IX_SeasonPeriods_PeriodEndDate",
            table: "SeasonPeriods");

        migrationBuilder.DropIndex(
            name: "IX_SeasonPeriods_PeriodStartDate",
            table: "SeasonPeriods");

        migrationBuilder.AlterColumn<string>(
            name: "Username",
            table: "Users",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(450)");

        migrationBuilder.AlterColumn<string>(
            name: "Email",
            table: "Users",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(450)");

        migrationBuilder.AlterColumn<string>(
            name: "TeamName",
            table: "Teams",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(450)");

        migrationBuilder.AlterColumn<string>(
            name: "SeasonName",
            table: "Seasons",
            type: "nvarchar(450)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.CreateIndex(
            name: "IX_Trades_UserId",
            table: "Trades",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Seasons_SeasonName_StartYear",
            table: "Seasons",
            columns: ["SeasonName", "StartYear"],
            unique: true);
    }
}
