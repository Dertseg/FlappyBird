using FlappyBird.Models;
namespace FlappyBird.Data;
public interface IGameStore
{
    void DiscardPendingResult(Guid id) { }
    long Revision { get; }
    event Action? ResultsChanged;
    Task InitializeAsync();
    Task<SaveOutcome> SaveResultAsync(GameResult result);
    Task<IReadOnlyList<GameResult>> GetTopAsync(Difficulty difficulty);
    Task<IReadOnlyList<GameResult>> GetRecentAsync(Difficulty difficulty);
    Task<GameResult?> GetLatestResultAsync();
    Task<HomeSummary> GetHomeSummaryAsync();
    Task<int> GetBestScoreAsync(Difficulty difficulty);
    Task<GameStatistics> GetStatisticsAsync();
    Task<IReadOnlyList<AchievementProgress>> GetAchievementsAsync();
    Task<PlayerSettings> GetSettingsAsync();
    Task SaveSettingsAsync(PlayerSettings settings);
    Task SaveBirdSkinAsync(string id);
    Task SaveFlightLocationAsync(string id);
}

public sealed record HomeSummary(PlayerSettings Settings, int BestScore, int Attempts, int UnlockedAchievements);
