namespace FlappyBird.Models;

public sealed record FlightLocation(string Id, string Name)
{
    public const string DefaultId = "meadow";

    public static IReadOnlyList<FlightLocation> All { get; } = Array.AsReadOnly<FlightLocation>(
    [
        new(DefaultId, "Классика"),
        new("coast", "Побережье"),
        new("peaks", "Горный перевал"),
        new("dusk", "Сумерки")
    ]);

    public static FlightLocation Get(string? id) => All.FirstOrDefault(location => location.Id == id) ?? All[0];
}
