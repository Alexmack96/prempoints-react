using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations;

/// <inheritdoc />
public partial class AddGameWeek : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.AddColumn<int>(
            name: "GameweekNumber",
            table: "SeasonPeriods",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.DropColumn(
            name: "GameweekNumber",
            table: "SeasonPeriods");
    }
}
