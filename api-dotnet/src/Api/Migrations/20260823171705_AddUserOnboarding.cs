using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FavouriteTeamId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UsernameChosen",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Users_FavouriteTeamId",
                table: "Users",
                column: "FavouriteTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Teams_FavouriteTeamId",
                table: "Users",
                column: "FavouriteTeamId",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Teams_FavouriteTeamId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_FavouriteTeamId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FavouriteTeamId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UsernameChosen",
                table: "Users");
        }
    }
}
