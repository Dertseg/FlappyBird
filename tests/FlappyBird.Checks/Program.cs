using FlappyBird.Models;
using FlappyBird.ViewModels;
using FlappyBird.Services;
using FlappyBird.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

int assertions = 0;
void Check(bool value, string message) { assertions++; if (!value) throw new Exception(message); }
GameDbContext Context(string path) => new(new DbContextOptionsBuilder<GameDbContext>().UseSqlite($"Data Source={path}").Options);
GameResult Result(int score, Difficulty difficulty = Difficulty.Normal) => new()
{ Id = Guid.NewGuid(), PlayerName = "Тест", Score = score, DurationSeconds = 12.5, Difficulty = difficulty, PlayedAtUtc = DateTime.UtcNow };

foreach (var option in DifficultyCatalog.All)
{
    var g = new GameEngine(7); g.Reset(option.Id);
    Check(g.GapSize == option.Gap && g.PipeSpeed == option.Speed, "Difficulty config reaches physics");
    var initialY = g.BirdY; g.Advance(1); Check(g.BirdY == initialY, "Ready does not simulate");
    g.Flap(); g.Advance(.02); Check(g.BirdY < initialY, "Tap moves upward");
    g.Pause(); var y = g.BirdY; var elapsed = g.Elapsed;
    g.Flap(); g.Advance(2); Check(g.BirdY == y && g.Elapsed == elapsed, "Pause freezes world");
    g.Resume(); g.Advance(.02); Check(g.Elapsed > elapsed, "Resume works");
    int lastScore = 0;
    for (int i = 0; i < 120 * 80 && g.State != GameState.GameOver && g.Score < 30; i++)
    {
        var next = g.Pipes.FirstOrDefault(p => p.X + GameEngine.PipeWidth >= GameEngine.BirdX - GameEngine.BirdRadius) ?? g.Pipes[^1];
        if (g.BirdY > next.GapY + 12 && g.Velocity >= 0) g.Flap();
        g.Advance(GameEngine.Step);
        Check(g.Score == lastScore || g.Score == lastScore + 1, "One point per pair"); lastScore = g.Score;
    }
    Check(g.Score >= 20, $"Course remains playable: {option.Id}: {g.Score}");
    g.Pause(); g.Finish(); Check(g.State == GameState.GameOver, "Finish paused attempt");
    var finalY = g.BirdY; g.Flap(); g.Advance(1); Check(g.BirdY == finalY, "Finished attempt frozen");
    var id = g.SessionId; g.Reset(option.Id); Check(g.SessionId != id && g.Score == 0, "New session on replay");
}
GameEngine Simulate(int hz) { var g = new GameEngine(1); g.Flap(); for (int i = 0; i < hz / 2; i++) g.Advance(1.0 / hz); return g; }
Check(Math.Abs(Simulate(30).BirdY - Simulate(120).BirdY) < .01, "Physics independent of render cadence");
Check(MedalProgress.Next(9).Contains("1 очк.") && MedalProgress.Next(10).Contains("15 очк."), "Next medal boundary");
Check(MedalProgress.Next(25).Contains("25 очк.") && MedalProgress.Fraction(100) == 1, "Silver and platinum thresholds");

var path = Path.Combine(Path.GetTempPath(), "flappy-v2-" + Guid.NewGuid() + ".db");
try
{
    // Build an actual version-1 SQLite database, then upgrade it with production initialization.
    var legacyId = Guid.NewGuid();
    using (var db = Context(path))
    {
        db.GetService<IMigrator>().Migrate("20261108130304_InitialCreate");
        db.Database.ExecuteSqlRaw("INSERT INTO GameResults (Id,PlayerName,Score,PlayedAtUtc,DurationSeconds,Mode) VALUES ({0},{1},{2},{3},{4},{5})",
            legacyId, "Старый игрок", 25, DateTime.UtcNow.AddDays(-1), 45.0, "Classic");
        db.Database.ExecuteSqlRaw("INSERT INTO PlayerSettings (Id,PlayerName,SoundEnabled,VibrationEnabled) VALUES (1,{0},0,0)", "Птица");
    }
    var store = new SqliteGameStore(path); await store.InitializeAsync(); await store.InitializeAsync();
    using (var db = Context(path))
    {
        Check(db.Database.GetAppliedMigrations().Count() == 3, "All migrations applied once");
        Check(!db.Database.HasPendingModelChanges(), "Snapshot matches current model");
        var legacy = db.Results.Single();
        Check(legacy.Id == legacyId && legacy.Score == 25 && legacy.Difficulty == Difficulty.Normal, "Legacy scores survive as Normal");
    }
    var settings = await store.GetSettingsAsync();
    Check(settings.PlayerName == "Птица" && !settings.SoundEnabled && settings.DefaultDifficulty == Difficulty.Normal, "Legacy settings preserved");
    var achievements = await store.GetAchievementsAsync();
    Check(achievements.Count(x => x.IsUnlocked) == 3, "Old scores grant earned achievements");
    var unlockDate = achievements[0].UnlockedAtUtc;
    var easy = Result(60, Difficulty.Easy);
    await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => store.SaveResultAsync(easy)));
    await store.SaveResultAsync(Result(5, Difficulty.Hard));
    Check((await store.GetTopAsync(Difficulty.Easy)).Single().Score == 60, "Easy isolated and retry idempotent");
    Check((await store.GetTopAsync(Difficulty.Normal)).Single().Score == 25, "Normal isolated");
    Check((await store.GetTopAsync(Difficulty.Hard)).Single().Score == 5, "Hard isolated");
    for (int i = 0; i < 15; i++) await store.SaveResultAsync(Result(i, Difficulty.Hard));
    var earlier = Result(14, Difficulty.Hard); earlier.PlayerName = "Раньше"; earlier.PlayedAtUtc = DateTime.UtcNow.AddDays(-1);
    await store.SaveResultAsync(earlier);
    var top = await store.GetTopAsync(Difficulty.Hard);
    Check(top.Count == 10 && top[0].PlayerName == "Раньше", "Top ten deterministic tie ordering");

    var nav = new FakeNavigation();
    var latest = Result(14, Difficulty.Hard);
    await store.SaveResultAsync(latest);
    var recordsStore = new FaultStore(new SqliteGameStore(path));
    var records = new LeaderboardViewModel(recordsStore);
    await records.LoadCommand.ExecuteAsync(null);
    Check(records.SelectedDifficulty == Difficulty.Hard && records.Leaders.Any(x => x.IsLatest) &&
        records.Leaders[0].Badge == "🥇", "Latest attempt highlights after reopening SQLite");
    var hardRows = records.Leaders;
    var hardNote = records.LastRunNote;
    recordsStore.FailTop = true;
    await records.SelectDifficultyCommand.ExecuteAsync("Easy");
    Check(records.SelectedDifficulty == Difficulty.Hard && ReferenceEquals(records.Leaders, hardRows) &&
        records.LastRunNote == hardNote && records.HasStatus, "Failed filter preserves difficulty, rows and note");
    recordsStore.FailTop = false;
    await records.SelectDifficultyCommand.ExecuteAsync("Easy");
    Check(records.Leaders.Single().Score == 60 && !records.Leaders[0].IsLatest, "Filter removes unrelated highlight");
    var low = Result(0, Difficulty.Hard); await store.SaveResultAsync(low);
    await records.LoadCommand.ExecuteAsync(null);
    Check(!records.Leaders.Any(x => x.IsLatest) && records.HasLastRunNote, "Non-top latest attempt still acknowledged");

    var failing = new FaultStore(store); var game = new GameViewModel(failing, new SilentFeedback(), nav);
    Check(await game.PrepareAsync(Difficulty.Normal), "Prepare game");
    Check(game.PreviousBest == 25, "Previous best captured before attempt");
    game.FlapCommand.Execute(null);
    for (int i = 0; i < 1000 && !game.IsGameOver; i++) game.Advance(GameEngine.Step);
    await game.SaveResultCommand.ExecutionTask!;
    Check(game.CanRetrySave && game.PreviousBest == 25 && game.DurationSeconds > 0, "Failure retains result metadata");
    var failedId = game.Engine.SessionId;
    await game.ReplayCommand.ExecuteAsync(null); await game.HomeCommand.ExecuteAsync(null);
    Check(game.Engine.SessionId == failedId && nav.Destinations.Count == 0, "Unsaved attempt cannot be lost via replay or navigation");
    var attemptsBefore = (await store.GetStatisticsAsync()).Attempts;
    await game.SaveResultCommand.ExecuteAsync(null); await game.SaveResultCommand.ExecuteAsync(null);
    Check(!game.CanRetrySave && (await store.GetStatisticsAsync()).Attempts == attemptsBefore + 1, "Retry records attempt once");
    Check(game.PreviousBest == 25 && (await store.GetLatestResultAsync())!.Id == failedId, "Previous best stable after save");
    await game.ReplayCommand.ExecuteAsync(null); game.FlapCommand.Execute(null); game.OnBackgrounded();
    Check(game.IsPaused, "Background pauses game");
    var resume = game.ResumeCommand.ExecuteAsync(null); game.OnBackgrounded(); await resume;
    Check(game.IsPaused && !game.HasCountdown, "Background cancels countdown");
    game.FinishCommand.Execute(null); await game.SaveResultCommand.ExecutionTask!;
    await game.HomeCommand.ExecuteAsync(null); Check(nav.Destinations.Last() == Destination.Home, "Saved game can navigate");
    int attemptsBeforeExit = (await store.GetStatisticsAsync()).Attempts;
    await game.ReplayCommand.ExecuteAsync(null);
    game.FlapCommand.Execute(null);
    game.HandleBack();
    Check(game.IsPaused && !game.IsChoosingExit, "First Back pauses");
    game.HandleBack();
    Check(game.IsChoosingExit, "Second Back offers exit options");
    game.HandleBack();
    Check(game.IsPaused && !game.IsChoosingExit, "Back closes exit options");
    game.ChooseExitCommand.Execute(null);
    await game.ExitWithoutSavingCommand.ExecuteAsync(null);
    Check(!game.IsChoosingExit && nav.Destinations.Last() == Destination.Home &&
        (await store.GetStatisticsAsync()).Attempts == attemptsBeforeExit, "Discarding a paused flight leaves no result");

    var settingsVm = new SettingsViewModel(store); await settingsVm.LoadCommand.ExecuteAsync(null);
    settingsVm.PlayerName = "  Сокол  "; settingsVm.SelectedDifficulty = DifficultyCatalog.Get(Difficulty.Easy);
    await settingsVm.SaveCommand.ExecuteAsync(null);
    Check((await store.GetSettingsAsync()).PlayerName == "Сокол", "Settings trim and persist name");
    var home = new HomeViewModel(store, nav, game); await home.LoadCommand.ExecuteAsync(null);
    Check(!home.NeedsReload, "Home data remains current until a successful write");
    nav.Fail = true;
    await home.AchievementsCommand.ExecuteAsync(null);
    Check(home.HasStatus && !home.IsBusy, "Home handles achievements navigation failure");
    await home.LeaderboardCommand.ExecuteAsync(null);
    Check(home.HasStatus && !home.IsBusy, "Home handles records navigation failure");
    nav.Fail = false;
    Check(home.SelectedDifficulty == Difficulty.Easy && home.BestScore == 60, "Home loads chosen difficulty best");
    await home.SelectDifficultyCommand.ExecuteAsync("Hard");
    Check(home.BestScore == 14 && (await store.GetSettingsAsync()).DefaultDifficulty == Difficulty.Hard, "Home changes persist and refresh matching best");

    Check((await store.GetSettingsAsync()).BirdSkinId == "sun" && home.Skins.Count == 7, "Existing profile starts with original skin");
    home.OpenSkinPickerCommand.Execute(null);
    home.DraftSkinId = "mint";
    home.CloseSkinPickerCommand.Execute(null);
    Check(home.SavedSkin.Id == "sun", "Unsaved selection does not change the badge");
    home.OpenSkinPickerCommand.Execute(null);
    Check(home.DraftSkinId == "sun", "Closing skin picker discards unsaved choice");
    home.DraftSkinId = "sky";
    await home.SaveSkinCommand.ExecuteAsync(null);
    Check(home.SavedSkin.Id == "sky" && home.NeedsReload, "Saving the skin changes the badge and invalidates cached data");
    await settingsVm.SaveCommand.ExecuteAsync(null);
    Check((await new SqliteGameStore(path).GetSettingsAsync()).BirdSkinId == "sky", "Skin survives restart and editing settings");
    Check(await game.PrepareAsync(Difficulty.Hard) && game.Skin.Id == "sky", "Flight uses the saved skin");
    await home.SelectDifficultyCommand.ExecuteAsync("Hard");

    var hardGoal = (await store.GetAchievementsAsync()).Single(x => x.Definition.Id == "hard_50");
    Check(!hardGoal.IsUnlocked && hardGoal.Current == 14, "Scores on other levels do not advance hard goal");
    await store.SaveResultAsync(Result(75, Difficulty.Normal));
    var hardUnlock = await store.SaveResultAsync(Result(50, Difficulty.Hard));
    Check(hardUnlock.NewAchievementIds.Contains("hard_50"), "Hard goal unlocks at its threshold");
    var attempts = (await store.GetStatisticsAsync()).Attempts;
    for (int i = attempts; i < 100; i++) await store.SaveResultAsync(Result(0));
    achievements = await store.GetAchievementsAsync();
    Check(achievements.All(x => x.IsUnlocked), "Hundred attempts achievement");
    Check((await store.GetStatisticsAsync()).Attempts == 100, "Duplicates do not contribute to attempts");
    await records.SelectListCommand.ExecuteAsync("History");
    await records.SelectDifficultyCommand.ExecuteAsync("Normal");
    Check(records.ShowHistory && records.Flights.Count == 20 &&
        records.FlightScores.SequenceEqual(records.Flights.Select(x => x.Score).Reverse()), "History is limited to 20 with chronological chart");
    await records.SelectDifficultyCommand.ExecuteAsync("Easy");
    Check(records.Flights.Count == 1 && records.Flights[0].Score == 60, "History filters by difficulty");
    var reopened = new SqliteGameStore(path); await reopened.InitializeAsync();
    Check((await reopened.GetAchievementsAsync())[0].UnlockedAtUtc == unlockDate, "Unlock dates survive reopening");
    Check((await reopened.GetAchievementsAsync()).All(x => x.IsUnlocked), "Achievements durable");
    Check((await reopened.GetSettingsAsync()).DefaultDifficulty == Difficulty.Hard, "Difficulty durable");
    var achievementsVm = new AchievementsViewModel(reopened); await achievementsVm.LoadCommand.ExecuteAsync(null);
    Check(achievementsVm.Completion == 1 && achievementsVm.Achievements.Count == AchievementCatalog.All.Count, "Achievements page progress");
    using (var db = Context(path)) Check(db.Achievements.Count() == AchievementCatalog.All.Count, "No duplicate unlock rows");
    bool rejected = false;
    try { await store.SaveResultAsync(Result(-1)); } catch (ArgumentException) { rejected = true; }
    Check(rejected, "Invalid score rejected");
}
finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path); }

// A fresh player earns the first achievement through the real GameViewModel save path.
var freshPath = Path.Combine(Path.GetTempPath(), "flappy-new-player-" + Guid.NewGuid() + ".db");
try
{
    var store = new SqliteGameStore(freshPath); await store.InitializeAsync();
    var nav = new FakeNavigation();
    var game = new GameViewModel(store, new SilentFeedback(), nav);
    Check(await game.PrepareAsync(Difficulty.Easy), "Fresh game prepares");
    game.FlapCommand.Execute(null);
    for (int i = 0; i < 1500 && !game.IsGameOver && game.Score < 1; i++)
    {
        var next = game.Engine.Pipes.FirstOrDefault(p => p.X + GameEngine.PipeWidth >= GameEngine.BirdX - GameEngine.BirdRadius) ?? game.Engine.Pipes[^1];
        if (game.Engine.BirdY > next.GapY + 12 && game.Engine.Velocity >= 0) game.FlapCommand.Execute(null);
        game.Advance(GameEngine.Step);
    }
    Check(game.Score == 1 && !game.IsGameOver, "Fresh player passes a pipe");
    game.PauseCommand.Execute(null); game.FinishCommand.Execute(null); await game.SaveResultCommand.ExecutionTask!;
    Check(game.ResultTitle == "Новый рекорд!" && game.PreviousBest == 0 && game.BestScore == 1, "New record compares against pre-attempt best");
    Check(game.HasNewAchievements && game.NewAchievementsText.Contains("Первое очко"), "Game result announces newly committed achievement");
    Check(game.NextMedalText.Contains("9 очк.") && (await store.GetLatestResultAsync())!.Difficulty == Difficulty.Easy, "Result medal and difficulty metadata");
    Check((await store.GetAchievementsAsync()).Count(x => x.IsUnlocked) == 1, "First achievement committed");
    await game.ReplayCommand.ExecuteAsync(null);
    Check(game.PreviousBest == 1 && game.IsReady && game.Engine.Difficulty == Difficulty.Easy, "Replay refreshes best and retains level");
    var failing = new GameViewModel(new FaultStore(store), new SilentFeedback(), nav);
    await failing.PrepareAsync(Difficulty.Hard); failing.FlapCommand.Execute(null);
    failing.PauseCommand.Execute(null); failing.FinishCommand.Execute(null); await failing.SaveResultCommand.ExecutionTask!;
    var id = failing.Engine.SessionId; failing.DiscardResultCommand.Execute(null); await failing.ReplayCommand.ExecuteAsync(null);
    Check(failing.IsReady && failing.Engine.SessionId != id && (await store.GetStatisticsAsync()).Attempts == 1, "Explicit discard permits replay without adding attempt");
}
finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(freshPath); }
var recoveryPath = Path.Combine(Path.GetTempPath(), "flappy-recovery-" + Guid.NewGuid() + ".db");
try
{
    var original = new SqliteGameStore(recoveryPath);
    await original.InitializeAsync();
    var pending = Result(10);
    using (var db = Context(recoveryPath)) db.Database.ExecuteSqlRaw("CREATE TRIGGER fail_result BEFORE INSERT ON GameResults BEGIN SELECT RAISE(ABORT, 'test failure'); END");
    bool failed = false;
    try { await original.SaveResultAsync(pending); } catch (DbUpdateException) { failed = true; }
    Check(failed && File.Exists(Path.Combine(recoveryPath + ".pending", pending.Id + ".json")), "SQLite failure leaves durable journal");
    using (var db = Context(recoveryPath)) db.Database.ExecuteSqlRaw("DROP TRIGGER fail_result");
    Directory.CreateDirectory(recoveryPath + ".pending");
    File.WriteAllText(Path.Combine(recoveryPath + ".pending", pending.Id + ".json"), System.Text.Json.JsonSerializer.Serialize(pending));
    var restored = new SqliteGameStore(recoveryPath);
    await restored.InitializeAsync();
    Check((await restored.GetTopAsync(Difficulty.Normal)).Single().Id == pending.Id, "Process restart recovers pending result");
    Check((await restored.GetAchievementsAsync()).Count(x => x.IsUnlocked) == 2, "Recovery grants achievements atomically");
    await restored.SaveResultAsync(pending);
    Check((await restored.GetStatisticsAsync()).Attempts == 1, "Recovery and retry do not duplicate result");
    var vm = new GameViewModel(restored, new ThrowingFeedback(), new FakeNavigation());
    await vm.PrepareAsync(Difficulty.Normal);
    vm.FlapCommand.Execute(null); vm.PauseCommand.Execute(null); vm.FinishCommand.Execute(null);
    await vm.SaveResultCommand.ExecutionTask!;
    Check(!vm.CanRetrySave && (await restored.GetStatisticsAsync()).Attempts == 2, "Feedback failures cannot prevent saving");
    var settings = new SettingsViewModel(restored);
    await settings.LoadCommand.ExecuteAsync(null);
    settings.PlayerName = "Черновик";
    await restored.SaveBirdSkinAsync("sky");
    if (settings.NeedsReload && !settings.IsDirty) await settings.LoadCommand.ExecuteAsync(null);
    Check(settings.PlayerName == "Черновик" && settings.IsDirty, "Unrelated writes preserve settings draft");
    await settings.SaveCommand.ExecuteAsync(null);
    Check(!settings.IsDirty && (await restored.GetSettingsAsync()).PlayerName == "Черновик", "Saving clears draft state");
}
finally
{
    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(recoveryPath);
    if (Directory.Exists(recoveryPath + ".pending")) Directory.Delete(recoveryPath + ".pending", true);
}
Console.WriteLine($"PASS: {assertions} assertions. Three difficulties, physics, five ViewModels, migration from v1, SQLite results and achievements.");

sealed class SilentFeedback : IGameFeedback { public void Play(string sound) { } public void Vibrate() { } }
sealed class FakeNavigation : INavigationService
{
    public List<Destination> Destinations { get; } = [];
    public bool Fail { get; set; }
    public Task GoToAsync(Destination destination)
    {
        if (Fail) throw new InvalidOperationException("Simulated navigation failure");
        Destinations.Add(destination);
        return Task.CompletedTask;
    }
}
sealed class FaultStore(IGameStore inner) : IGameStore
{
    public long Revision => inner.Revision;
    private bool fail = true;
    public bool FailTop { get; set; }
    public Task InitializeAsync() => inner.InitializeAsync();
    public Task<IReadOnlyList<GameResult>> GetTopAsync(Difficulty difficulty) =>
        FailTop ? Task.FromException<IReadOnlyList<GameResult>>(new IOException("Simulated query failure")) : inner.GetTopAsync(difficulty);
    public Task<GameResult?> GetLatestResultAsync() => inner.GetLatestResultAsync();
    public Task<IReadOnlyList<GameResult>> GetRecentAsync(Difficulty difficulty) => inner.GetRecentAsync(difficulty);
    public Task<HomeSummary> GetHomeSummaryAsync() => inner.GetHomeSummaryAsync();
    public Task<int> GetBestScoreAsync(Difficulty difficulty) => inner.GetBestScoreAsync(difficulty);
    public Task<GameStatistics> GetStatisticsAsync() => inner.GetStatisticsAsync();
    public Task<IReadOnlyList<AchievementProgress>> GetAchievementsAsync() => inner.GetAchievementsAsync();
    public Task<PlayerSettings> GetSettingsAsync() => inner.GetSettingsAsync();
    public Task SaveSettingsAsync(PlayerSettings settings) => inner.SaveSettingsAsync(settings);
    public Task SaveBirdSkinAsync(string id) => inner.SaveBirdSkinAsync(id);
    public Task<SaveOutcome> SaveResultAsync(GameResult result)
    {
        if (fail) { fail = false; throw new IOException("Simulated disk failure"); }
        return inner.SaveResultAsync(result);
    }
}

sealed class ThrowingFeedback : IGameFeedback { public void Play(string sound) => throw new InvalidOperationException("Audio failure"); public void Vibrate() => throw new InvalidOperationException("Haptic failure"); }
