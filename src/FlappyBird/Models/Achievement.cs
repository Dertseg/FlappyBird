namespace FlappyBird.Models;
public sealed class Achievement
{
    public string Id { get; set; } = "";
    public DateTime UnlockedAtUtc { get; set; }
}
public sealed record GameStatistics(int Attempts, int BestScore);
public sealed record SaveOutcome(IReadOnlyList<string> NewAchievementIds);
public sealed record AchievementDefinition(string Id, string Title, string Description, int Goal, bool UsesAttempts, string Symbol);
public sealed record AchievementProgress(AchievementDefinition Definition, int Current, DateTime? UnlockedAtUtc)
{
    public string Title => Definition.Title;
    public string Description => Definition.Description;
    public string Symbol => Definition.Symbol;
    public bool IsUnlocked => UnlockedAtUtc.HasValue;
    public double Progress => Math.Clamp((double)Current / Definition.Goal, 0, 1);
    public string ProgressText => $"{Math.Min(Current, Definition.Goal)} / {Definition.Goal}";
    public string StateText => IsUnlocked ? $"Получено {UnlockedAtUtc!.Value.ToLocalTime():dd.MM.yyyy}" : "Ещё впереди";
}
public static class AchievementCatalog
{
    public static IReadOnlyList<AchievementDefinition> All { get; } =
    [
        new("first_point", "Первое очко", "Пройди первую пару труб за одну попытку", 1, false, "01"),
        new("ten_pipes", "10 труб", "Набери 10 очков за одну попытку", 10, false, "10"),
        new("twenty_five_pipes", "25 труб", "Набери 25 очков за одну попытку", 25, false, "25"),
        new("hundred_attempts", "100 попыток", "Заверши 100 полётов на любом уровне", 100, true, "100")
    ];
    public static int Current(AchievementDefinition definition, GameStatistics stats) => definition.UsesAttempts ? stats.Attempts : stats.BestScore;
}
