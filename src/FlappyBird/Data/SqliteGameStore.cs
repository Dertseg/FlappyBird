using FlappyBird.Models;
using Microsoft.EntityFrameworkCore;
namespace FlappyBird.Data;
/// <summary>Serialized SQLite access; each result and its achievements commit atomically.</summary>
public sealed class SqliteGameStore(string databasePath) : IGameStore
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    private readonly DbContextOptions<GameDbContext> options = new DbContextOptionsBuilder<GameDbContext>()
        .UseSqlite(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = databasePath, ForeignKeys = true }.ToString()).Options;
    private async Task<T> RunAsync<T>(Func<GameDbContext, T> action)
    {
        await gate.WaitAsync();
        try
        {
            return await Task.Run(() =>
            {
                using var db = new GameDbContext(options);
                EnsureInitialized(db);
                return action(db);
            });
        }
        finally { gate.Release(); }
    }
    private static IQueryable<GameResult> ClassicResults(GameDbContext db) =>
        db.Results.AsNoTracking().Where(x => x.Mode == "Classic");

    private static GameStatistics Statistics(GameDbContext db)
    {
        var levels = ClassicResults(db).GroupBy(x => x.Difficulty)
            .Select(group => new { Difficulty = group.Key, Attempts = group.Count(), Best = group.Max(x => x.Score) })
            .ToArray();
        return new GameStatistics(levels.Sum(x => x.Attempts), levels.Select(x => x.Best).DefaultIfEmpty().Max())
        {
            BestByDifficulty = levels.ToDictionary(x => x.Difficulty, x => x.Best)
        };
    }
    private static IReadOnlyList<string> Unlock(GameDbContext db)
    {
        var stats = Statistics(db);
        var unlocked = db.Achievements.Select(x => x.Id).ToHashSet();
        var earned = AchievementCatalog.All.Where(x => AchievementCatalog.Current(x, stats) >= x.Goal && !unlocked.Contains(x.Id)).ToArray();
        foreach (var definition in earned) db.Achievements.Add(new Achievement { Id = definition.Id, UnlockedAtUtc = DateTime.UtcNow });
        db.SaveChanges(); return earned.Select(x => x.Id).ToArray();
    }
    public Task InitializeAsync() => RunAsync(_ => true);

    private void EnsureInitialized(GameDbContext db)
    {
        if (initialized) return;
        db.Database.Migrate();
        using var transaction = db.Database.BeginTransaction();
        Unlock(db); // Existing attempts count when upgrading from version 1.
        transaction.Commit();
        initialized = true;
    }
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
        return await RunAsync(db => (IReadOnlyList<GameResult>)ClassicResults(db)
            .Where(x => x.Difficulty == difficulty)
            .OrderByDescending(x => x.Score).ThenBy(x => x.PlayedAtUtc).ThenBy(x => x.Id).Take(10).ToArray());
    }
    public Task<GameResult?> GetLatestResultAsync() => RunAsync(db => ClassicResults(db)
        .OrderByDescending(x => x.PlayedAtUtc).ThenByDescending(x => x.Id).FirstOrDefault());

    public Task<IReadOnlyList<GameResult>> GetRecentAsync(Difficulty difficulty)
    {
        _ = DifficultyCatalog.Get(difficulty);
        return RunAsync(db => (IReadOnlyList<GameResult>)ClassicResults(db).Where(x => x.Difficulty == difficulty)
            .OrderByDescending(x => x.PlayedAtUtc).ThenByDescending(x => x.Id).Take(20).ToArray());
    }

    private static int BestScore(GameDbContext db, Difficulty difficulty) => ClassicResults(db)
        .Where(x => x.Difficulty == difficulty).Select(x => (int?)x.Score).Max() ?? 0;

    public Task<int> GetBestScoreAsync(Difficulty difficulty)
    {
        _ = DifficultyCatalog.Get(difficulty);
        return RunAsync(db => BestScore(db, difficulty));
    }

    public Task<HomeSummary> GetHomeSummaryAsync() => RunAsync(db =>
    {
        var settings = ReadSettings(db);
        return new HomeSummary(settings, BestScore(db, settings.DefaultDifficulty),
            ClassicResults(db).Count(), db.Achievements.Count());
    });
    public async Task<GameStatistics> GetStatisticsAsync() => await RunAsync(Statistics);
    public async Task<IReadOnlyList<AchievementProgress>> GetAchievementsAsync() => await RunAsync(db =>
    {
        var stats = Statistics(db);
        var unlocked = db.Achievements.AsNoTracking().ToDictionary(x => x.Id, x => x.UnlockedAtUtc);
        return (IReadOnlyList<AchievementProgress>)AchievementCatalog.All.Select(x => new AchievementProgress(x,
            AchievementCatalog.Current(x, stats), unlocked.TryGetValue(x.Id, out var date) ? date : null)).ToArray();
    });
    private static PlayerSettings ReadSettings(GameDbContext db) =>
        db.Settings.AsNoTracking().SingleOrDefault(x => x.Id == 1) ?? new PlayerSettings();

    public Task<PlayerSettings> GetSettingsAsync() => RunAsync(ReadSettings);
    public async Task SaveSettingsAsync(PlayerSettings settings)
    {
        _ = DifficultyCatalog.Get(settings.DefaultDifficulty);
        if (string.IsNullOrWhiteSpace(settings.PlayerName) || settings.PlayerName.Trim().Length > 20) throw new ArgumentException("Invalid player name");
        await RunAsync(db =>
        {
            var saved = EditSettings(db);
            saved.PlayerName = settings.PlayerName.Trim();
            saved.DefaultDifficulty = settings.DefaultDifficulty;
            saved.SoundEnabled = settings.SoundEnabled;
            saved.VibrationEnabled = settings.VibrationEnabled;
            db.SaveChanges(); return true;
        });
    }

    public Task SaveBirdSkinAsync(string id)
    {
        if (BirdSkin.Get(id).Id != id) throw new ArgumentException("Invalid bird skin", nameof(id));
        return RunAsync(db =>
        {
            EditSettings(db).BirdSkinId = id;
            db.SaveChanges();
            return true;
        });
    }

    private static PlayerSettings EditSettings(GameDbContext db) =>
        db.Settings.Find(1) ?? db.Settings.Add(new PlayerSettings()).Entity;
}
