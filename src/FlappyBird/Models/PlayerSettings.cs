namespace FlappyBird.Models;

public sealed class PlayerSettings
{
    public Difficulty DefaultDifficulty { get; set; } = Difficulty.Normal;
    public int Id { get; set; } = 1;
    public string PlayerName { get; set; } = "Игрок";
    public bool SoundEnabled { get; set; } = true;
    public bool VibrationEnabled { get; set; } = true;
    public string BirdSkinId { get; set; } = "sun";
}
