using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable


namespace Api.Migrations;

/// <inheritdoc />
public partial class InitCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.CreateTable(
            name: "Seasons",
            columns: table => new
            {
                SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SeasonName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Seasons", x => x.SeasonId);
            });

        migrationBuilder.CreateTable(
            name: "Teams",
            columns: table => new
            {
                TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TeamName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Teams", x => x.TeamId);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Role = table.Column<int>(type: "int", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.UserId);
            });

        migrationBuilder.CreateTable(
            name: "SeasonPeriods",
            columns: table => new
            {
                SeasonPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PeriodStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                PeriodEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SeasonPeriods", x => x.SeasonPeriodId);
                table.ForeignKey(
                    name: "FK_SeasonPeriods_Seasons_SeasonId",
                    column: x => x.SeasonId,
                    principalTable: "Seasons",
                    principalColumn: "SeasonId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TeamSeasons",
            columns: table => new
            {
                TeamSeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TeamSeasons", x => x.TeamSeasonId);
                table.ForeignKey(
                    name: "FK_TeamSeasons_Seasons_SeasonId",
                    column: x => x.SeasonId,
                    principalTable: "Seasons",
                    principalColumn: "SeasonId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TeamSeasons_Teams_TeamId",
                    column: x => x.TeamId,
                    principalTable: "Teams",
                    principalColumn: "TeamId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "UserSeasons",
            columns: table => new
            {
                UserSeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                LateJoinerFee = table.Column<int>(type: "int", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserSeasons", x => x.UserSeasonId);
                table.ForeignKey(
                    name: "FK_UserSeasons_Seasons_SeasonId",
                    column: x => x.SeasonId,
                    principalTable: "Seasons",
                    principalColumn: "SeasonId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UserSeasons_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Prices",
            columns: table => new
            {
                PriceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SeasonPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                ValueDate = table.Column<DateOnly>(type: "date", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Prices", x => x.PriceId);
                table.ForeignKey(
                    name: "FK_Prices_SeasonPeriods_SeasonPeriodId",
                    column: x => x.SeasonPeriodId,
                    principalTable: "SeasonPeriods",
                    principalColumn: "SeasonPeriodId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Prices_Teams_TeamId",
                    column: x => x.TeamId,
                    principalTable: "Teams",
                    principalColumn: "TeamId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Trades",
            columns: table => new
            {
                TradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PriceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SeasonPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Exposure = table.Column<int>(type: "int", nullable: false),
                TradeType = table.Column<int>(type: "int", nullable: false),
                TimezoneIana = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Trades", x => x.TradeId);
                table.ForeignKey(
                    name: "FK_Trades_Prices_PriceId",
                    column: x => x.PriceId,
                    principalTable: "Prices",
                    principalColumn: "PriceId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Trades_SeasonPeriods_SeasonPeriodId",
                    column: x => x.SeasonPeriodId,
                    principalTable: "SeasonPeriods",
                    principalColumn: "SeasonPeriodId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Trades_Teams_TeamId",
                    column: x => x.TeamId,
                    principalTable: "Teams",
                    principalColumn: "TeamId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Trades_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "UserId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Prices_SeasonPeriodId",
            table: "Prices",
            column: "SeasonPeriodId");

        migrationBuilder.CreateIndex(
            name: "IX_Prices_TeamId",
            table: "Prices",
            column: "TeamId");

        migrationBuilder.CreateIndex(
            name: "IX_SeasonPeriods_SeasonId",
            table: "SeasonPeriods",
            column: "SeasonId");

        migrationBuilder.CreateIndex(
            name: "IX_TeamSeasons_SeasonId",
            table: "TeamSeasons",
            column: "SeasonId");

        migrationBuilder.CreateIndex(
            name: "IX_TeamSeasons_TeamId",
            table: "TeamSeasons",
            column: "TeamId");

        migrationBuilder.CreateIndex(
            name: "IX_Trades_PriceId",
            table: "Trades",
            column: "PriceId");

        migrationBuilder.CreateIndex(
            name: "IX_Trades_SeasonPeriodId",
            table: "Trades",
            column: "SeasonPeriodId");

        migrationBuilder.CreateIndex(
            name: "IX_Trades_TeamId",
            table: "Trades",
            column: "TeamId");

        migrationBuilder.CreateIndex(
            name: "IX_Trades_UserId",
            table: "Trades",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserSeasons_SeasonId",
            table: "UserSeasons",
            column: "SeasonId");

        migrationBuilder.CreateIndex(
            name: "IX_UserSeasons_UserId",
            table: "UserSeasons",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.DropTable(
            name: "TeamSeasons");

        migrationBuilder.DropTable(
            name: "Trades");

        migrationBuilder.DropTable(
            name: "UserSeasons");

        migrationBuilder.DropTable(
            name: "Prices");

        migrationBuilder.DropTable(
            name: "Users");

        migrationBuilder.DropTable(
            name: "SeasonPeriods");

        migrationBuilder.DropTable(
            name: "Teams");

        migrationBuilder.DropTable(
            name: "Seasons");
    }
}
