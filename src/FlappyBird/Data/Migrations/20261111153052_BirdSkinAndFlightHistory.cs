using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlappyBird.Data.Migrations
{
    /// <inheritdoc />
    public partial class BirdSkinAndFlightHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BirdSkinId",
                table: "PlayerSettings",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "sun");

            migrationBuilder.CreateIndex(
                name: "IX_GameResults_Mode_Difficulty_PlayedAtUtc_Id",
                table: "GameResults",
                columns: new[] { "Mode", "Difficulty", "PlayedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameResults_Mode_Difficulty_PlayedAtUtc_Id",
                table: "GameResults");

            migrationBuilder.DropColumn(
                name: "BirdSkinId",
                table: "PlayerSettings");
        }
    }
}
