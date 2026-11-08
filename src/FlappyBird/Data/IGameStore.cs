using FlappyBird.Models;
namespace FlappyBird.Data;

public interface IGameStore
{
    Task InitializeAsync();
    Task SaveResultAsync(GameResult result);
    Task<IReadOnlyList<GameResult>> GetTopAsync();
    Task<PlayerSettings> GetSettingsAsync();
    Task SaveSettingsAsync(PlayerSettings settings);
}
