using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FlappyBird.Data.Migrations;

[DbContext(typeof(GameDbContext))]
[Migration("20261113010000_ResultLocation")]
public sealed class ResultLocationMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>("FlightLocationId", "GameResults", "TEXT", maxLength: 16, nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("FlightLocationId", "GameResults");
}
