using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlappyBird.Data.Migrations
{
    /// <inheritdoc />
    public partial class DifficultyAndAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameResults_Mode_Score_PlayedAtUtc",
                table: "GameResults");

            migrationBuilder.AddColumn<string>(
                name: "DefaultDifficulty",
                table: "PlayerSettings",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "GameResults",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    UnlockedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameResults_Mode_Difficulty_Score_PlayedAtUtc",
                table: "GameResults",
                columns: new[] { "Mode", "Difficulty", "Score", "PlayedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropIndex(
                name: "IX_GameResults_Mode_Difficulty_Score_PlayedAtUtc",
                table: "GameResults");

            migrationBuilder.DropColumn(
                name: "DefaultDifficulty",
                table: "PlayerSettings");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "GameResults");

            migrationBuilder.CreateIndex(
                name: "IX_GameResults_Mode_Score_PlayedAtUtc",
                table: "GameResults",
                columns: new[] { "Mode", "Score", "PlayedAtUtc" });
        }
    }
}
