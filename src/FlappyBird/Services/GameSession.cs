using FlappyBird.Models;
namespace FlappyBird.Services;
/// <summary>Last saved attempt for UI highlighting; durable data lives in SQLite.</summary>
public sealed class GameSession { public GameResult? LastSavedResult { get; set; } }
