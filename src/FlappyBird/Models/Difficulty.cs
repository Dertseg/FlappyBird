namespace FlappyBird.Models;
public enum Difficulty { Normal = 0, Easy = 1, Hard = 2 }
public sealed record DifficultyOption(Difficulty Id, string Name, string Description, double Speed, double Gap);
public static class DifficultyCatalog
{
    public static IReadOnlyList<DifficultyOption> All { get; } =
    [
        new(Difficulty.Easy, "Лёгкий", "Больше пространства для первого полёта", 110, 200),
        new(Difficulty.Normal, "Обычный", "Классический ритм и честный вызов", 130, 166),
        new(Difficulty.Hard, "Сложный", "Быстрые трубы. Точные движения.", 155, 144)
    ];
    public static DifficultyOption Get(Difficulty difficulty) => All.FirstOrDefault(x => x.Id == difficulty)
        ?? throw new ArgumentOutOfRangeException(nameof(difficulty));
}
