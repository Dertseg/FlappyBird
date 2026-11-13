using FlappyBird.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace FlappyBird.Data;
public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<GameResult> Results => Set<GameResult>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<PlayerSettings> Settings => Set<PlayerSettings>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<GameResult>(e =>
        {
            e.ToTable("GameResults"); e.HasKey(x => x.Id);
            e.Property(x => x.PlayerName).HasMaxLength(20).IsRequired();
            e.Property(x => x.Mode).HasMaxLength(20).IsRequired();
            e.Property(x => x.FlightLocationId).HasMaxLength(16);
            e.Property(x => x.Difficulty).HasConversion<string>().HasMaxLength(16).HasDefaultValue(Difficulty.Normal);
            e.HasIndex(x => new { x.Mode, x.Difficulty, x.Score, x.PlayedAtUtc });
            e.HasIndex(x => new { x.Mode, x.Difficulty, x.PlayedAtUtc, x.Id });
        });
        model.Entity<Achievement>(e =>
        {
            e.ToTable("Achievements"); e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(40);
        });
        model.Entity<PlayerSettings>(e =>
        {
            e.ToTable("PlayerSettings"); e.HasKey(x => x.Id);
            e.Property(x => x.DefaultDifficulty).HasConversion<string>().HasMaxLength(16).HasDefaultValue(Difficulty.Normal);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.PlayerName).HasMaxLength(20).IsRequired();
            e.Property(x => x.BirdSkinId).HasMaxLength(16).HasDefaultValue("sun");
            e.Property(x => x.FlightLocationId).HasMaxLength(16).HasDefaultValue(FlightLocation.DefaultId);
        });
    }
}
public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<GameDbContext>
{
    public GameDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<GameDbContext>().UseSqlite("Data Source=flappybird-design.db").Options);
}
