using FlappyBird.Models;
using Microsoft.EntityFrameworkCore;
namespace FlappyBird.Data;
/// <summary>Serialized SQLite access; each result and its achievements commit atomically.</summary>
public sealed class SqliteGameStore(string databasePath) : IGameStore
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    private GameDbContext CreateContext() => new(new DbContextOptionsBuilder<GameDbContext>()
        .UseSqlite(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = databasePath, ForeignKeys = true }.ToString()).Options);
    private async Task<T> RunAsync<T>(Func<GameDbContext, T> action)
    {
        await gate.WaitAsync();
        try { return await Task.Run(() => { using var db = CreateContext(); return action(db); }); }
        finally { gate.Release(); }
    }
    private static GameStatistics Statistics(GameDbContext db) => new(db.Results.Count(x => x.Mode == "Classic"),
        db.Results.Where(x => x.Mode == "Classic").Select(x => (int?)x.Score).Max() ?? 0);
    private static IReadOnlyList<string> Unlock(GameDbContext db)
    {
        var stats = Statistics(db);
        var unlocked = db.Achievements.Select(x => x.Id).ToHashSet();
        var earned = AchievementCatalog.All.Where(x => AchievementCatalog.Current(x, stats) >= x.Goal && !unlocked.Contains(x.Id)).ToArray();
        foreach (var definition in earned) db.Achievements.Add(new Achievement { Id = definition.Id, UnlockedAtUtc = DateTime.UtcNow });
        db.SaveChanges(); return earned.Select(x => x.Id).ToArray();
    }
    public async Task InitializeAsync() => await RunAsync(db =>
    {
        if (initialized) return true;
        db.Database.Migrate();
        using var transaction = db.Database.BeginTransaction();
        Unlock(db); // Existing attempts count when upgrading from version 1.
        transaction.Commit(); initialized = true; return true;
    });
    public async Task<SaveOutcome> SaveResultAsync(GameResult result)
    {
        if (result.Id == Guid.Empty || result.Score < 0 || !double.IsFinite(result.DurationSeconds) || result.DurationSeconds < 0 ||
            !Enum.IsDefined(result.Difficulty) || result.Mode != "Classic") throw new ArgumentException("Invalid game result");
        return await RunAsync(db =>
        {
            using var transaction = db.Database.BeginTransaction();
            if (db.Results.Any(x => x.Id == result.Id)) return new SaveOutcome([]);
            db.Results.Add(result); db.SaveChanges();
            var achievements = Unlock(db);
            transaction.Commit(); return new SaveOutcome(achievements);
        });
    }
    public async Task<IReadOnlyList<GameResult>> GetTopAsync(Difficulty difficulty)
    {
        _ = DifficultyCatalog.Get(difficulty);
        return await RunAsync(db => (IReadOnlyList<GameResult>)db.Results.AsNoTracking()
            .Where(x => x.Mode == "Classic" && x.Difficulty == difficulty)
            .OrderByDescending(x => x.Score).ThenBy(x => x.PlayedAtUtc).ThenBy(x => x.Id).Take(10).ToArray());
    }
    public async Task<GameStatistics> GetStatisticsAsync() => await RunAsync(Statistics);
    public async Task<IReadOnlyList<AchievementProgress>> GetAchievementsAsync() => await RunAsync(db =>
    {
        var stats = Statistics(db);
        var unlocked = db.Achievements.AsNoTracking().ToDictionary(x => x.Id, x => x.UnlockedAtUtc);
        return (IReadOnlyList<AchievementProgress>)AchievementCatalog.All.Select(x => new AchievementProgress(x,
            AchievementCatalog.Current(x, stats), unlocked.TryGetValue(x.Id, out var date) ? date : null)).ToArray();
    });
    public async Task<PlayerSettings> GetSettingsAsync() => await RunAsync(db => db.Settings.AsNoTracking().SingleOrDefault(x => x.Id == 1) ?? new PlayerSettings());
    public async Task SaveSettingsAsync(PlayerSettings settings)
    {
        _ = DifficultyCatalog.Get(settings.DefaultDifficulty);
        if (string.IsNullOrWhiteSpace(settings.PlayerName) || settings.PlayerName.Trim().Length > 20) throw new ArgumentException("Invalid player name");
        await RunAsync(db =>
        {
            settings.Id = 1; settings.PlayerName = settings.PlayerName.Trim();
            if (db.Settings.Any(x => x.Id == 1)) db.Settings.Update(settings); else db.Settings.Add(settings);
            db.SaveChanges(); return true;
        });
    }
}
