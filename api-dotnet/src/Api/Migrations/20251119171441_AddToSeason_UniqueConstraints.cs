using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations;

/// <inheritdoc />
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "<Pending>")]
public partial class AddToSeasonUniqueConstraints : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.AlterColumn<string>(
            name: "SeasonName",
            table: "Seasons",
            type: "nvarchar(450)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.CreateIndex(
            name: "IX_Seasons_SeasonName_StartYear",
            table: "Seasons",
            columns: ["SeasonName", "StartYear"],
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.DropIndex(
            name: "IX_Seasons_SeasonName_StartYear",
            table: "Seasons");

        migrationBuilder.AlterColumn<string>(
            name: "SeasonName",
            table: "Seasons",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(450)");
    }
}
