namespace FlappyBird.Models;

public sealed class GameResult
{
    public Guid Id { get; set; }
    public string PlayerName { get; set; } = "Игрок";
    public int Score { get; set; }
    public DateTime PlayedAtUtc { get; set; }
    public double DurationSeconds { get; set; }
    public string Mode { get; set; } = "Classic";
}
