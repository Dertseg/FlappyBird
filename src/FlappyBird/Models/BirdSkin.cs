namespace FlappyBird.Models;

public sealed record BirdSkin(string Id, string Name, string Body, string Wing, string Shade)
{
    public static IReadOnlyList<BirdSkin> All { get; } =
    [
        new("sun", "Солнечная", "#FFD56A", "#F4B544", "#DAA53D"),
        new("mint", "Мятная", "#82DFB6", "#35B68C", "#27896C"),
        new("sky", "Небесная", "#84CFFF", "#419ADE", "#3376AD"),
        new("berry", "Ягодная", "#F68CBA", "#D95791", "#A94478"),
        new("lilac", "Сиреневая", "#C8ACFF", "#A17BEE", "#7457BC"),
        new("peach", "Персиковая", "#FFB888", "#EE8B62", "#B76E52"),
        new("storm", "Грозовая", "#A7B7CB", "#677C9A", "#435772")
    ];

    public static BirdSkin Get(string id) => All.FirstOrDefault(skin => skin.Id == id) ?? All[0];
}
