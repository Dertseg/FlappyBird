using FlappyBird.Models;
using Microsoft.EntityFrameworkCore;
namespace FlappyBird.Data;
/// <summary>SQLite operations run serially off the UI thread, each with its own context.</summary>
public sealed class SqliteGameStore(string databasePath) : IGameStore
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private GameDbContext CreateContext() => new(new DbContextOptionsBuilder<GameDbContext>()
        .UseSqlite(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = databasePath, ForeignKeys = true }.ToString()).Options);
    private async Task<T> RunAsync<T>(Func<GameDbContext, T> action)
    {
        await gate.WaitAsync();
        try { return await Task.Run(() => { using var db = CreateContext(); return action(db); }); }
        finally { gate.Release(); }
    }
    public async Task InitializeAsync() => await RunAsync(db => { db.Database.Migrate(); return true; });
    public async Task SaveResultAsync(GameResult result)
    {
        if (result.Id == Guid.Empty || result.Score < 0 || !double.IsFinite(result.DurationSeconds) || result.DurationSeconds < 0)
            throw new ArgumentException("Invalid game result");
        await RunAsync(db =>
        {
            // Stable session GUID makes retries idempotent, including after a lost response.
            if (db.Results.Any(x => x.Id == result.Id)) return true;
            db.Results.Add(result); db.SaveChanges(); return true;
        });
    }
    public async Task<IReadOnlyList<GameResult>> GetTopAsync() => await RunAsync(db =>
        (IReadOnlyList<GameResult>)db.Results.AsNoTracking().Where(x => x.Mode == "Classic")
            .OrderByDescending(x => x.Score).ThenBy(x => x.PlayedAtUtc).ThenBy(x => x.Id).Take(10).ToArray());
    public async Task<PlayerSettings> GetSettingsAsync() => await RunAsync(db => db.Settings.AsNoTracking().SingleOrDefault(x => x.Id == 1) ?? new PlayerSettings());
    public async Task SaveSettingsAsync(PlayerSettings settings) => await RunAsync(db =>
    {
        settings.Id = 1;
        if (db.Settings.Any(x => x.Id == 1)) db.Settings.Update(settings); else db.Settings.Add(settings);
        db.SaveChanges(); return true;
    });
}
