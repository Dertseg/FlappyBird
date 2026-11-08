namespace FlappyBird.Models;
public sealed record ScoreRow(int Place, string Player, int Score, string Date, bool IsLatest)
{
    public string Badge => Place switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => Place.ToString("00") };
    public bool IsPodium => Place <= 3;
}
