using FlappyBird.Core;
using FlappyBird.Data;
using Microsoft.EntityFrameworkCore;

int assertions = 0;
void Check(bool value, string message) { assertions++; if (!value) throw new Exception(message); }
var game = new GameEngine(42);
double initial = game.BirdY;
game.Advance(1); Check(game.BirdY == initial, "Ready does not simulate");
game.Flap(); game.Advance(0.02); Check(game.BirdY < initial, "Tap gives upward impulse");
game.Pause(); double y = game.BirdY; double time = game.Elapsed;
game.Advance(5); game.Flap(); Check(game.BirdY == y && game.Elapsed == time, "Pause freezes world and input");
game.Resume(); game.Advance(0.02); Check(game.Elapsed > time, "Resume restarts simulation");
for (int i = 0; i < 1000 && game.State != GameState.GameOver; i++) game.Advance(GameEngine.Step);
Check(game.State == GameState.GameOver, "Ground collision ends game");
double finalY = game.BirdY; int finalScore = game.Score;
game.Flap(); game.Advance(0.1);
Check(game.BirdY == finalY && game.Score == finalScore, "Finished attempt ignores input and simulation");
var oldSession = game.SessionId; game.Reset(); Check(game.Score == 0 && game.SessionId != oldSession, "Reset creates a distinct attempt");

// Autopilot exercises actual generated pipes, score transitions and recycling for 30 points.
game = new GameEngine(7);
int lastScore = 0;
for (int i = 0; i < 120 * 70 && game.State != GameState.GameOver; i++)
{
    var next = game.Pipes.FirstOrDefault(p => p.X + GameEngine.PipeWidth >= GameEngine.BirdX - GameEngine.BirdRadius) ?? game.Pipes[^1];
    if (game.State == GameState.Ready || (game.BirdY > next.GapY + 12 && game.Velocity >= 0)) game.Flap();
    game.Advance(GameEngine.Step);
    Check(game.Score == lastScore || game.Score == lastScore + 1, "Score increases at most once per pipe"); lastScore = game.Score;
    Check(game.Pipes.All(p => p.GapY is >= 175 and <= 410), "Gap stays inside safe range");
}
Check(game.Score >= 30, $"Generated course is playable: {game.Score} points, state {game.State}");

// Simulation is independent of render cadence.
GameEngine Simulate(int hz)
{
    var g = new GameEngine(1); g.Flap();
    for (int i = 0; i < hz / 2; i++) g.Advance(1.0 / hz);
    return g;
}
var slow = Simulate(30); var fast = Simulate(120);
Check(Math.Abs(slow.BirdY - fast.BirdY) < 0.01, "30 and 120 Hz produce same physics");

string path = Path.Combine(Path.GetTempPath(), "flappy-check-" + Guid.NewGuid() + ".db");
try
{
    var store = new SqliteGameStore(path); await store.InitializeAsync(); await store.InitializeAsync();
    using (var db = new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite($"Data Source={path}").Options))
    {
        Check(db.Database.GetAppliedMigrations().Count() == 1, "Initial migration applied once");
        Check(!db.Database.HasPendingModelChanges(), "Migration snapshot matches EF model");
    }
    var result = new GameResult { Id = Guid.NewGuid(), PlayerName = "Тест", Score = 42, PlayedAtUtc = DateTime.UtcNow, DurationSeconds = 12 };
    await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => store.SaveResultAsync(result)));
    Check((await store.GetTopAsync()).Count == 1, "Concurrent save retries do not duplicate session");
    await store.SaveSettingsAsync(new PlayerSettings { PlayerName = "Птица", SoundEnabled = false, VibrationEnabled = false });
    var reopened = new SqliteGameStore(path); await reopened.InitializeAsync();
    Check((await reopened.GetTopAsync()).Single().Score == 42, "Result survives reopening SQLite");
    Check((await reopened.GetSettingsAsync()).PlayerName == "Птица", "Settings survive reopening SQLite");
    for (int i = 0; i < 15; i++) await store.SaveResultAsync(new GameResult { Id = Guid.NewGuid(), Score = i, PlayedAtUtc = DateTime.UtcNow });
    await store.SaveResultAsync(new GameResult { Id = Guid.NewGuid(), Score = 42, PlayerName = "Раньше", PlayedAtUtc = DateTime.UtcNow.AddDays(-1) });
    var top = await store.GetTopAsync();
    Check(top.Count == 10 && top[0].PlayerName == "Раньше" && top[1].Score == 42, "Top ten ordered by score then earliest date");
    var failure = new FaultStore(reopened); var vm = new MainViewModel(failure, new SilentFeedback());
    int attemptsBefore;
    using (var db = new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite($"Data Source={path}").Options)) attemptsBefore = db.Results.Count();
    await vm.InitializeAsync(); await vm.StartCommand.ExecuteAsync(null); vm.FlapCommand.Execute(null);
    for (int i = 0; i < 1000 && !vm.IsGameOver; i++) vm.Advance(GameEngine.Step);
    await vm.SaveResultCommand.ExecutionTask!;
    Check(vm.CanRetrySave, "Save failure leaves explicit retry option");
    await vm.StartCommand.ExecuteAsync(null); Check(vm.IsGameOver, "Unsaved result cannot be silently discarded by restart");
    await vm.SaveResultCommand.ExecuteAsync(null); Check(!vm.CanRetrySave, "Retry saves pending result");
    for (int i = 0; i < 20; i++) vm.Advance(GameEngine.Step);
    await vm.SaveResultCommand.ExecuteAsync(null);
    using (var db = new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite($"Data Source={path}").Options))
        Check(db.Results.Count() == attemptsBefore + 1, "Repeated finished frames and commands save one attempt");
    Check((await store.GetTopAsync()).Count == 10, "Leaderboard remains bounded after retry");
    await vm.StartCommand.ExecuteAsync(null); vm.FlapCommand.Execute(null); vm.OnBackgrounded();
    Check(vm.IsPaused && vm.Engine.State == GameState.Paused, "Backgrounding pauses game");
    var resume = vm.ResumeCommand.ExecuteAsync(null); vm.OnBackgrounded(); await resume;
    Check(vm.IsPaused && !vm.HasCountdown, "Backgrounding cancels resume countdown");
}
finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path); }
Console.WriteLine($"PASS: {assertions} assertions, physics + MVVM + real EF Core/SQLite migrations and persistence.");

sealed class SilentFeedback : IGameFeedback { public void Play(string sound) { } public void Vibrate() { } }
sealed class FaultStore(IGameStore inner) : IGameStore
{
    private bool fail = true;
    public Task InitializeAsync() => inner.InitializeAsync();
    public Task<IReadOnlyList<GameResult>> GetTopAsync() => inner.GetTopAsync();
    public Task<PlayerSettings> GetSettingsAsync() => inner.GetSettingsAsync();
    public Task SaveSettingsAsync(PlayerSettings settings) => inner.SaveSettingsAsync(settings);
    public Task SaveResultAsync(GameResult result)
    {
        if (fail) { fail = false; throw new IOException("Simulated disk failure"); }
        return inner.SaveResultAsync(result);
    }
}
