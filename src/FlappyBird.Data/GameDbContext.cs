using FlappyBird.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace FlappyBird.Data;
public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<GameResult> Results => Set<GameResult>();
    public DbSet<PlayerSettings> Settings => Set<PlayerSettings>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<GameResult>(e =>
        {
            e.ToTable("GameResults"); e.HasKey(x => x.Id);
            e.Property(x => x.PlayerName).HasMaxLength(20).IsRequired();
            e.Property(x => x.Mode).HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.Mode, x.Score, x.PlayedAtUtc });
        });
        model.Entity<PlayerSettings>(e =>
        {
            e.ToTable("PlayerSettings"); e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.PlayerName).HasMaxLength(20).IsRequired();
        });
    }
}
public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<GameDbContext>
{
    public GameDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<GameDbContext>().UseSqlite("Data Source=flappybird-design.db").Options);
}
