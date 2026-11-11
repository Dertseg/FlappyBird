using FlappyBird.Models;
namespace FlappyBird.Data;
public interface IGameStore
{
    Task InitializeAsync();
    Task<SaveOutcome> SaveResultAsync(GameResult result);
    Task<IReadOnlyList<GameResult>> GetTopAsync(Difficulty difficulty);
    Task<GameResult?> GetLatestResultAsync();
    Task<HomeSummary> GetHomeSummaryAsync();
    Task<int> GetBestScoreAsync(Difficulty difficulty);
    Task<GameStatistics> GetStatisticsAsync();
    Task<IReadOnlyList<AchievementProgress>> GetAchievementsAsync();
    Task<PlayerSettings> GetSettingsAsync();
    Task SaveSettingsAsync(PlayerSettings settings);
}

public sealed record HomeSummary(PlayerSettings Settings, int BestScore, int Attempts, int UnlockedAchievements);
