using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations;

/// <inheritdoc />
public partial class AddToSeasonStartYear : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.AddColumn<int>(
            name: "StartYear",
            table: "Seasons",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.DropColumn(
            name: "StartYear",
            table: "Seasons");
    }
}
