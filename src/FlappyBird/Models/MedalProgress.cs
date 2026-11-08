namespace FlappyBird.Models;
public static class MedalProgress
{
    public static string Earned(int score) => score switch
    {
        >= 100 => "Платиновая медаль", >= 50 => "Золотая медаль", >= 25 => "Серебряная медаль",
        >= 10 => "Бронзовая медаль", _ => "Первый шаг сделан"
    };
    public static string Next(int score) => score switch
    {
        < 10 => $"Ещё {10 - score} очк. до бронзовой медали",
        < 25 => $"Ещё {25 - score} очк. до серебряной медали",
        < 50 => $"Ещё {50 - score} очк. до золотой медали",
        < 100 => $"Ещё {100 - score} очк. до платиновой медали",
        _ => "Все медали этого полёта собраны!"
    };
    public static double Fraction(int score)
    {
        var (start, end) = score switch { < 10 => (0, 10), < 25 => (10, 25), < 50 => (25, 50), < 100 => (50, 100), _ => (100, 100) };
        return start == end ? 1 : (double)(score - start) / (end - start);
    }
}
