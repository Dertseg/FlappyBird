namespace FlappyBird.Models;

public sealed class PlayerSettings
{
    public int Id { get; set; } = 1;
    public string PlayerName { get; set; } = "Игрок";
    public bool SoundEnabled { get; set; } = true;
    public bool VibrationEnabled { get; set; } = true;
}
