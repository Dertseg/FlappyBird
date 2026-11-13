namespace FlappyBird.Models;
public sealed class Achievement
{
    public string Id { get; set; } = "";
    public DateTime UnlockedAtUtc { get; set; }
}
public sealed record GameStatistics(int Attempts, int BestScore)
{
    public IReadOnlyDictionary<Difficulty, int> BestByDifficulty { get; init; } = new Dictionary<Difficulty, int>();
    public IReadOnlyDictionary<string, LocationStatistics> ByLocation { get; init; } = new Dictionary<string, LocationStatistics>();
}
public sealed record LocationStatistics(int Attempts, int BestScore);
public sealed record SaveOutcome(IReadOnlyList<string> NewAchievementIds);
public sealed record AchievementDefinition(string Id, string Title, string Description, int Goal, bool UsesAttempts,
    string Icon, Difficulty? Difficulty = null, string? LocationId = null);
public sealed record AchievementProgress(AchievementDefinition Definition, int Current, DateTime? UnlockedAtUtc)
{
    public string Title => Definition.Title;
    public string Description => Definition.Description;
    public string Icon => $"award_{Definition.Icon}.png";
    public bool IsUnlocked => UnlockedAtUtc.HasValue;
    public double Progress => Math.Clamp((double)Current / Definition.Goal, 0, 1);
    public string ProgressText => $"{Math.Min(Current, Definition.Goal)} / {Definition.Goal}";
    public string StateText => IsUnlocked ? $"Получено {UnlockedAtUtc!.Value.ToLocalTime():dd.MM.yyyy}" : "Ещё впереди";
}
public static class AchievementCatalog
{
    public static IReadOnlyList<AchievementDefinition> All { get; } =
    [
        new("first_point", "Первое очко", "Пройди первую пару труб за одну попытку", 1, false, "spark"),
        new("ten_pipes", "10 труб", "Набери 10 очков за одну попытку", 10, false, "pipes"),
        new("twenty_five_pipes", "25 труб", "Набери 25 очков за одну попытку", 25, false, "flag"),
        new("hundred_attempts", "100 попыток", "Заверши 100 полётов на любом уровне", 100, true, "calendar"),
        new("easy_50", "Спокойный полёт", "Набери 50 очков за один лёгкий полёт", 50, false, "feather", Difficulty.Easy),
        new("normal_75", "На одном дыхании", "Набери 75 очков за один обычный полёт", 75, false, "wind", Difficulty.Normal),
        new("hard_50", "Мастер виражей", "Набери 50 очков за один сложный полёт", 50, false, "bolt", Difficulty.Hard),
        new("meadow_5", "Над лугом", "Набери 5 очков за полёт в Классике", 5, false, "sprout", LocationId: "meadow"),
        new("meadow_20", "Знакомый маршрут", "Набери 20 очков за полёт в Классике", 20, false, "trail", LocationId: "meadow"),
        new("coast_1", "Морской бриз", "Набери первое очко на Побережье", 1, false, "wave", LocationId: "coast"),
        new("coast_10", "Вдоль берега", "Набери 10 очков за полёт на Побережье", 10, false, "sail", LocationId: "coast"),
        new("coast_runs", "Смотритель маяка", "Заверши 10 полётов на Побережье", 10, true, "beacon", LocationId: "coast"),
        new("peaks_1", "У подножия", "Набери первое очко на Горном перевале", 1, false, "mountain", LocationId: "peaks"),
        new("peaks_15", "Выше облаков", "Набери 15 очков за полёт на Горном перевале", 15, false, "summit", LocationId: "peaks"),
        new("dusk_1", "Первый вечер", "Набери первое очко в Сумерках", 1, false, "sunset", LocationId: "dusk"),
        new("dusk_10", "Под первой звездой", "Набери 10 очков за полёт в Сумерках", 10, false, "night", LocationId: "dusk"),
        new("dusk_runs", "Вечерняя привычка", "Заверши 10 полётов в Сумерках", 10, true, "owl", LocationId: "dusk")
    ];
    public static int Current(AchievementDefinition definition, GameStatistics stats) =>
        definition.LocationId is { } location
            ? stats.ByLocation.TryGetValue(location, out var local) ? (definition.UsesAttempts ? local.Attempts : local.BestScore) : 0
            : definition.Difficulty is { } difficulty ? stats.BestByDifficulty.GetValueOrDefault(difficulty) :
        definition.UsesAttempts ? stats.Attempts : stats.BestScore;
}
