namespace FlappyBird.Core;

public sealed class GameResult
{
    public Guid Id { get; set; }
    public string PlayerName { get; set; } = "Игрок";
    public int Score { get; set; }
    public DateTime PlayedAtUtc { get; set; }
    public double DurationSeconds { get; set; }
    public string Mode { get; set; } = "Classic";
}

public sealed class PlayerSettings
{
    public int Id { get; set; } = 1;
    public string PlayerName { get; set; } = "Игрок";
    public bool SoundEnabled { get; set; } = true;
    public bool VibrationEnabled { get; set; } = true;
}

public interface IGameStore
{
    Task InitializeAsync();
    Task SaveResultAsync(GameResult result);
    Task<IReadOnlyList<GameResult>> GetTopAsync();
    Task<PlayerSettings> GetSettingsAsync();
    Task SaveSettingsAsync(PlayerSettings settings);
}

public interface IGameFeedback
{
    void Play(string sound);
    void Vibrate();
}
