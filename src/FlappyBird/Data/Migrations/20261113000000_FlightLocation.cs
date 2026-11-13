using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FlappyBird.Data.Migrations;

[DbContext(typeof(GameDbContext))]
[Migration("20261113000000_FlightLocation")]
public sealed class FlightLocationMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(
            name: "FlightLocationId", table: "PlayerSettings", type: "TEXT",
            maxLength: 16, nullable: false, defaultValue: "meadow");

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "FlightLocationId", table: "PlayerSettings");
}
